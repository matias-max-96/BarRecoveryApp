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
    [Route("api/sync/shipments")]
    public class ShipmentsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ShipmentsController(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        [HttpGet]
        public async Task<ActionResult<List<ShipmentSyncDto>>> GetChangedSince(
            [FromQuery] DateTime? since)
        {
            var query = _db.Shipments
                .Include(x => x.Bars)
                .AsQueryable();

            if (since.HasValue)
            {
                query = query.Where(x => x.UpdatedAtUtc > since.Value);
            }

            var shipments = await query
                .OrderBy(x => x.UpdatedAtUtc)
                .ToListAsync();

            return Ok(shipments.Select(ToDto).ToList());
        }

        // Push idempotente, sin LWW: un Shipment nunca se edita después de
        // creado (no existe ningún método de actualización en el service).
        [HttpPost]
        public async Task<IActionResult> Push([FromBody] ShipmentSyncDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return BadRequest("El envío debe traer un Id (generado por la tablet).");

            var alreadyExists = await _db.Shipments
                .AnyAsync(x => x.Id == dto.Id);

            if (alreadyExists)
            {
                return Ok(dto);
            }

            var entity = new Shipment
            {
                Id = dto.Id,
                TransferOrder = dto.TransferOrder,
                CustomerReference = dto.CustomerReference,
                DispatchGuideNumber = dto.DispatchGuideNumber,
                ShippedAtUtc = dto.ShippedAtUtc,
                ResponsibleUserId = dto.ResponsibleUserId,
                IsActive = dto.IsActive,
                CreatedAtUtc = dto.CreatedAtUtc,
                UpdatedAtUtc = dto.UpdatedAtUtc,
                Bars = dto.BarIds.Select(barId => new ShipmentBar
                {
                    Id = Guid.NewGuid().ToString(),
                    ShipmentId = dto.Id,
                    BarId = barId,
                    IsActive = true,
                    CreatedAtUtc = dto.CreatedAtUtc,
                    UpdatedAtUtc = dto.UpdatedAtUtc
                }).ToList()
            };

            _db.Shipments.Add(entity);

            await _db.SaveChangesAsync();

            return Ok(dto);
        }

        private static ShipmentSyncDto ToDto(Shipment shipment)
        {
            return new ShipmentSyncDto
            {
                Id = shipment.Id,
                TransferOrder = shipment.TransferOrder,
                CustomerReference = shipment.CustomerReference,
                DispatchGuideNumber = shipment.DispatchGuideNumber,
                ShippedAtUtc = shipment.ShippedAtUtc,
                ResponsibleUserId = shipment.ResponsibleUserId,
                IsActive = shipment.IsActive,
                CreatedAtUtc = shipment.CreatedAtUtc,
                UpdatedAtUtc = shipment.UpdatedAtUtc,
                BarIds = shipment.Bars.Select(b => b.BarId).ToList()
            };
        }
    }
}