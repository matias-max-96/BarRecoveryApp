using BarRecoveryApp.Api.Data;
using BarRecoveryApp.Api.Dtos;
using BarRecoveryApp.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarRecoveryApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/sync/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsersController(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        [HttpGet]
        public async Task<ActionResult<List<UserSyncDto>>> GetChangedSince(
            [FromQuery] DateTime? since)
        {
            var query = _db.Users.AsQueryable();

            if (since.HasValue)
            {
                query = query.Where(x => x.UpdatedAtUtc > since.Value);
            }

            var result = await query
                .OrderBy(x => x.UpdatedAtUtc)
                .Select(x => ToDto(x))
                .ToListAsync();

            return Ok(result);
        }

        // Push con Last-Write-Wins, mismo patrón que Plant/Bar.
        [HttpPost]
        public async Task<IActionResult> Push([FromBody] UserSyncDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return BadRequest("El registro debe traer un Id (generado por la tablet).");

            var existing = await _db.Users.FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (existing is null)
            {
                _db.Users.Add(new User
                {
                    Id = dto.Id,
                    Username = dto.Username,
                    DisplayName = dto.DisplayName,
                    RoleCode = dto.RoleCode,
                    PinHash = dto.PinHash,
                    PinSalt = dto.PinSalt,
                    IsPinEnabled = dto.IsPinEnabled,
                    MustChangePin = dto.MustChangePin,
                    CreatedByUserId = dto.CreatedByUserId,
                    UpdatedByUserId = dto.UpdatedByUserId,
                    IsActive = dto.IsActive,
                    CreatedAtUtc = dto.CreatedAtUtc,
                    UpdatedAtUtc = dto.UpdatedAtUtc
                });

                await _db.SaveChangesAsync();

                return Ok(dto);
            }

            if (dto.UpdatedAtUtc <= existing.UpdatedAtUtc)
            {
                return Conflict(new UserSyncConflictDto
                {
                    ServerVersion = ToDto(existing)
                });
            }

            existing.Username = dto.Username;
            existing.DisplayName = dto.DisplayName;
            existing.RoleCode = dto.RoleCode;
            existing.PinHash = dto.PinHash;
            existing.PinSalt = dto.PinSalt;
            existing.IsPinEnabled = dto.IsPinEnabled;
            existing.MustChangePin = dto.MustChangePin;
            existing.CreatedByUserId = dto.CreatedByUserId;
            existing.UpdatedByUserId = dto.UpdatedByUserId;
            existing.IsActive = dto.IsActive;
            existing.UpdatedAtUtc = dto.UpdatedAtUtc;

            await _db.SaveChangesAsync();

            return Ok(ToDto(existing));
        }

        // Endpoint liviano para la verificación oportunista post-login: no
        // expone el usuario completo (ni su PinHash), solo si sigue activo.
        [HttpGet("{userId}/active-status")]
        public async Task<ActionResult<UserActiveStatusDto>> GetActiveStatus(string userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);

            return Ok(new UserActiveStatusDto
            {
                IsActive = user?.IsActive
            });
        }

        private static UserSyncDto ToDto(User user)
        {
            return new UserSyncDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                RoleCode = user.RoleCode,
                PinHash = user.PinHash,
                PinSalt = user.PinSalt,
                IsPinEnabled = user.IsPinEnabled,
                MustChangePin = user.MustChangePin,
                CreatedByUserId = user.CreatedByUserId,
                UpdatedByUserId = user.UpdatedByUserId,
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc
            };
        }
    }
}