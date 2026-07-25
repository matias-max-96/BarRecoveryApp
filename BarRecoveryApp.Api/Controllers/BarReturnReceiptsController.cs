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
    [Route("api/sync/bar-return-receipts")]
    public class BarReturnReceiptsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BarReturnReceiptsController(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        [HttpGet]
        public async Task<ActionResult<List<BarReturnReceiptSyncDto>>> GetChangedSince(
            [FromQuery] DateTime? since)
        {
            var query = _db.BarReturnReceipts
                .Include(x => x.Bars)
                .AsQueryable();

            if (since.HasValue)
            {
                query = query.Where(x => x.UpdatedAtUtc > since.Value);
            }

            var receipts = await query
                .OrderBy(x => x.UpdatedAtUtc)
                .ToListAsync();

            return Ok(receipts.Select(ToDto).ToList());
        }

        // Push idempotente, sin LWW: un BarReturnReceipt nunca se edita
        // después de creado (no existe ningún método de actualización en
        // el service).
        [HttpPost]
        public async Task<IActionResult> Push([FromBody] BarReturnReceiptSyncDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return BadRequest("El recibo debe traer un Id (generado por la tablet).");

            var alreadyExists = await _db.BarReturnReceipts
                .AnyAsync(x => x.Id == dto.Id);

            if (alreadyExists)
            {
                return Ok(dto);
            }

            var entity = new BarReturnReceipt
            {
                Id = dto.Id,
                ReturnDocument = dto.ReturnDocument,
                ReceivedAtUtc = dto.ReceivedAtUtc,
                ResponsibleUserId = dto.ResponsibleUserId,
                Notes = dto.Notes,
                IsActive = dto.IsActive,
                CreatedAtUtc = dto.CreatedAtUtc,
                UpdatedAtUtc = dto.UpdatedAtUtc,
                Bars = dto.BarIds.Select(barId => new BarReturnReceiptBar
                {
                    Id = Guid.NewGuid().ToString(),
                    BarReturnReceiptId = dto.Id,
                    BarId = barId,
                    IsActive = true,
                    CreatedAtUtc = dto.CreatedAtUtc,
                    UpdatedAtUtc = dto.UpdatedAtUtc
                }).ToList()
            };

            _db.BarReturnReceipts.Add(entity);

            await _db.SaveChangesAsync();

            return Ok(dto);
        }

        private static BarReturnReceiptSyncDto ToDto(BarReturnReceipt receipt)
        {
            return new BarReturnReceiptSyncDto
            {
                Id = receipt.Id,
                ReturnDocument = receipt.ReturnDocument,
                ReceivedAtUtc = receipt.ReceivedAtUtc,
                ResponsibleUserId = receipt.ResponsibleUserId,
                Notes = receipt.Notes,
                IsActive = receipt.IsActive,
                CreatedAtUtc = receipt.CreatedAtUtc,
                UpdatedAtUtc = receipt.UpdatedAtUtc,
                BarIds = receipt.Bars.Select(b => b.BarId).ToList()
            };
        }
    }
}