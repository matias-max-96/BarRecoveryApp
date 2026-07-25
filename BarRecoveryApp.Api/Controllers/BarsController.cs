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
    [Route("api/sync/bars")]
    public class BarsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BarsController(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        [HttpGet]
        public async Task<ActionResult<List<BarSyncDto>>> GetChangedSince(
            [FromQuery] DateTime? since)
        {
            var query = _db.Bars.AsQueryable();

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

        // Push con Last-Write-Wins: Bar sí se edita después de creada
        // (BarService.SaveBarAsync permite editar, y QualityInspection /
        // Shipment / BarReturnReceipt la mutan también). Solo una persona
        // ajusta recuperaciones a la vez (confirmado con el cliente), así
        // que LWW simple es suficiente — sin necesidad de lógica de
        // incremento para RecoveryCount.
        [HttpPost]
        public async Task<IActionResult> Push([FromBody] BarSyncDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return BadRequest("El registro debe traer un Id (generado por la tablet).");

            var existing = await _db.Bars.FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (existing is null)
            {
                _db.Bars.Add(new Bar
                {
                    Id = dto.Id,
                    BarNumber = dto.BarNumber,
                    PlantId = dto.PlantId,
                    BarTypeId = dto.BarTypeId,
                    CurrentStatus = dto.CurrentStatus,
                    RecoveryCount = dto.RecoveryCount,
                    IsDisposed = dto.IsDisposed,
                    IsActive = dto.IsActive,
                    CreatedAtUtc = dto.CreatedAtUtc,
                    UpdatedAtUtc = dto.UpdatedAtUtc
                });

                await _db.SaveChangesAsync();

                return Ok(dto);
            }

            if (dto.UpdatedAtUtc <= existing.UpdatedAtUtc)
            {
                return Conflict(new BarSyncConflictDto
                {
                    ServerVersion = ToDto(existing)
                });
            }

            existing.BarNumber = dto.BarNumber;
            existing.PlantId = dto.PlantId;
            existing.BarTypeId = dto.BarTypeId;
            existing.CurrentStatus = dto.CurrentStatus;
            existing.RecoveryCount = dto.RecoveryCount;
            existing.IsDisposed = dto.IsDisposed;
            existing.IsActive = dto.IsActive;
            existing.UpdatedAtUtc = dto.UpdatedAtUtc;

            await _db.SaveChangesAsync();

            return Ok(ToDto(existing));
        }

        private static BarSyncDto ToDto(Bar bar)
        {
            return new BarSyncDto
            {
                Id = bar.Id,
                BarNumber = bar.BarNumber,
                PlantId = bar.PlantId,
                BarTypeId = bar.BarTypeId,
                CurrentStatus = bar.CurrentStatus,
                RecoveryCount = bar.RecoveryCount,
                IsDisposed = bar.IsDisposed,
                IsActive = bar.IsActive,
                CreatedAtUtc = bar.CreatedAtUtc,
                UpdatedAtUtc = bar.UpdatedAtUtc
            };
        }
    }
}