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
    [Route("api/sync/plants")]
    public class PlantsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PlantsController(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // Pull incremental: la tablet manda el LastPulledAtUtc que tiene
        // guardado en su tabla local SyncState para "Plant", y solo recibe
        // lo que cambió desde entonces. Nunca se baja la tabla completa.
        [HttpGet]
        public async Task<ActionResult<List<PlantSyncDto>>> GetChangedSince(
            [FromQuery] DateTime? since)
        {
            var query = _db.Plants.AsQueryable();

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

 
        [HttpPost]
        public async Task<IActionResult> Push([FromBody] PlantSyncDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return BadRequest("El registro debe traer un Id (generado por la tablet).");

            var existing = await _db.Plants.FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (existing is null)
            {
                _db.Plants.Add(new Plant
                {
                    Id = dto.Id,
                    Code = dto.Code,
                    Name = dto.Name,
                    Description = dto.Description,
                    IsActive = dto.IsActive,
                    CreatedAtUtc = dto.CreatedAtUtc,
                    UpdatedAtUtc = dto.UpdatedAtUtc
                });

                await _db.SaveChangesAsync();

                return Ok(dto);
            }

            if (dto.UpdatedAtUtc <= existing.UpdatedAtUtc)
            {
                // El servidor ya tiene algo igual o más nuevo — el cambio de
                // esta tablet pierde. Se le devuelve la versión del servidor.
                return Conflict(new SyncConflictDto
                {
                    ServerVersion = ToDto(existing)
                });
            }

            existing.Code = dto.Code;
            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.IsActive = dto.IsActive;
            existing.UpdatedAtUtc = dto.UpdatedAtUtc;

            await _db.SaveChangesAsync();

            return Ok(ToDto(existing));
        }

        private static PlantSyncDto ToDto(Plant plant)
        {
            return new PlantSyncDto
            {
                Id = plant.Id,
                Code = plant.Code,
                Name = plant.Name,
                Description = plant.Description,
                IsActive = plant.IsActive,
                CreatedAtUtc = plant.CreatedAtUtc,
                UpdatedAtUtc = plant.UpdatedAtUtc
            };
        }
    }
}
