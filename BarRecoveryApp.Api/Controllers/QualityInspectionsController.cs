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
    [Route("api/sync/quality-inspections")]
    public class QualityInspectionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public QualityInspectionsController(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        [HttpGet]
        public async Task<ActionResult<List<QualityInspectionSyncDto>>> GetChangedSince(
            [FromQuery] DateTime? since)
        {
            var query = _db.QualityInspections
                .Include(x => x.AttributeValues)
                .AsQueryable();

            if (since.HasValue)
            {
                query = query.Where(x => x.UpdatedAtUtc > since.Value);
            }

            var inspections = await query
                .OrderBy(x => x.UpdatedAtUtc)
                .ToListAsync();

            return Ok(inspections.Select(ToDto).ToList());
        }

        // Push idempotente, sin LWW: una QualityInspection nunca se edita
        // después de creada (confirmado en QualityInspectionService — no
        // existe ningún método de actualización).
        [HttpPost]
        public async Task<IActionResult> Push([FromBody] QualityInspectionSyncDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return BadRequest("La inspección debe traer un Id (generado por la tablet).");

            var alreadyExists = await _db.QualityInspections
                .AnyAsync(x => x.Id == dto.Id);

            if (alreadyExists)
            {
                return Ok(dto);
            }

            var entity = new QualityInspection
            {
                Id = dto.Id,
                BarId = dto.BarId,
                InspectorUserId = dto.InspectorUserId,
                InspectionAtUtc = dto.InspectionAtUtc,
                RecoveryCountAtInspection = dto.RecoveryCountAtInspection,
                CanBeRecovered = dto.CanBeRecovered,
                MustBeDisposed = dto.MustBeDisposed,
                IsApprovedForShipment = dto.IsApprovedForShipment,
                Notes = dto.Notes,
                IsActive = dto.IsActive,
                CreatedAtUtc = dto.CreatedAtUtc,
                UpdatedAtUtc = dto.UpdatedAtUtc,
                AttributeValues = dto.AttributeValues.Select(a => new QualityInspectionAttributeValue
                {
                    Id = a.Id,
                    QualityInspectionId = dto.Id,
                    BarId = a.BarId,
                    AttributeDefinitionId = a.AttributeDefinitionId,
                    AttributeCode = a.AttributeCode,
                    AttributeName = a.AttributeName,
                    DataType = a.DataType,
                    WasMeasured = a.WasMeasured,
                    ValueText = a.ValueText,
                    ValueNumber = a.ValueNumber,
                    ValueDate = a.ValueDate,
                    ValueBool = a.ValueBool,
                    IsOutOfRange = a.IsOutOfRange,
                    MinValueAtInspection = a.MinValueAtInspection,
                    MaxValueAtInspection = a.MaxValueAtInspection,
                    UnitAtInspection = a.UnitAtInspection,
                    ToleranceTextAtInspection = a.ToleranceTextAtInspection,
                    IsActive = true,
                    CreatedAtUtc = dto.CreatedAtUtc,
                    UpdatedAtUtc = dto.UpdatedAtUtc
                }).ToList()
            };

            _db.QualityInspections.Add(entity);

            await _db.SaveChangesAsync();

            return Ok(dto);
        }

        private static QualityInspectionSyncDto ToDto(QualityInspection inspection)
        {
            return new QualityInspectionSyncDto
            {
                Id = inspection.Id,
                BarId = inspection.BarId,
                InspectorUserId = inspection.InspectorUserId,
                InspectionAtUtc = inspection.InspectionAtUtc,
                RecoveryCountAtInspection = inspection.RecoveryCountAtInspection,
                CanBeRecovered = inspection.CanBeRecovered,
                MustBeDisposed = inspection.MustBeDisposed,
                IsApprovedForShipment = inspection.IsApprovedForShipment,
                Notes = inspection.Notes,
                IsActive = inspection.IsActive,
                CreatedAtUtc = inspection.CreatedAtUtc,
                UpdatedAtUtc = inspection.UpdatedAtUtc,
                AttributeValues = inspection.AttributeValues.Select(a => new QualityInspectionAttributeValueSyncDto
                {
                    Id = a.Id,
                    BarId = a.BarId,
                    AttributeDefinitionId = a.AttributeDefinitionId,
                    AttributeCode = a.AttributeCode,
                    AttributeName = a.AttributeName,
                    DataType = a.DataType,
                    WasMeasured = a.WasMeasured,
                    ValueText = a.ValueText,
                    ValueNumber = a.ValueNumber,
                    ValueDate = a.ValueDate,
                    ValueBool = a.ValueBool,
                    IsOutOfRange = a.IsOutOfRange,
                    MinValueAtInspection = a.MinValueAtInspection,
                    MaxValueAtInspection = a.MaxValueAtInspection,
                    UnitAtInspection = a.UnitAtInspection,
                    ToleranceTextAtInspection = a.ToleranceTextAtInspection
                }).ToList()
            };
        }
    }
}