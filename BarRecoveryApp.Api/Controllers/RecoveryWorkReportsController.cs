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
    [Route("api/sync/recovery-work-reports")]
    public class RecoveryWorkReportsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RecoveryWorkReportsController(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // Pull incremental: trae el agregado completo (reporte + categorías
        // + actividades + insumos) de cada reporte cambiado desde "since".
        [HttpGet]
        public async Task<ActionResult<List<RecoveryWorkReportSyncDto>>> GetChangedSince(
            [FromQuery] DateTime? since)
        {
            var query = _db.RecoveryWorkReports
                .Include(x => x.Categories).ThenInclude(x => x.Activities)
                .Include(x => x.Categories).ThenInclude(x => x.Supplies)
                .AsQueryable();

            if (since.HasValue)
            {
                query = query.Where(x => x.UpdatedAtUtc > since.Value);
            }

            var reports = await query
                .OrderBy(x => x.UpdatedAtUtc)
                .ToListAsync();

            return Ok(reports.Select(ToDto).ToList());
        }

        // Push: a diferencia de Plant, un RecoveryWorkReport nunca se edita
        // después de creado (confirmado en RecoveryWorkReportService — no
        // existe ningún método de actualización). Por eso el push acá es
        // simplemente "insertar si no existe" — no hay Last-Write-Wins
        // porque no puede haber dos versiones distintas del mismo reporte,
        // solo existe o no existe.
        [HttpPost]
        public async Task<IActionResult> Push([FromBody] RecoveryWorkReportSyncDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return BadRequest("El reporte debe traer un Id (generado por la tablet).");

            var alreadyExists = await _db.RecoveryWorkReports
                .AnyAsync(x => x.Id == dto.Id);

            if (alreadyExists)
            {
                // Ya lo tenemos (otra tablet lo subió antes, o es un reintento
                // de esta misma) — no es un error, simplemente no hay nada
                // que hacer.
                return Ok(dto);
            }

            var entity = new RecoveryWorkReport
            {
                Id = dto.Id,
                UserId = dto.UserId,
                WorkDate = dto.WorkDate,
                ShiftName = dto.ShiftName,
                Notes = dto.Notes,
                IsActive = dto.IsActive,
                CreatedAtUtc = dto.CreatedAtUtc,
                UpdatedAtUtc = dto.UpdatedAtUtc,
                Categories = dto.Categories.Select(c => new RecoveryWorkReportCategory
                {
                    Id = c.Id,
                    RecoveryWorkReportId = dto.Id,
                    WorkType = c.WorkType,
                    PlantId = c.PlantId,
                    BarTypeId = c.BarTypeId,
                    BarsWorkedCount = c.BarsWorkedCount,
                    ExportLabel = c.ExportLabel,
                    IsActive = true,
                    CreatedAtUtc = dto.CreatedAtUtc,
                    UpdatedAtUtc = dto.UpdatedAtUtc,
                    Activities = c.Activities.Select(a => new RecoveryWorkActivity
                    {
                        Id = a.Id,
                        RecoveryWorkReportCategoryId = c.Id,
                        ActivityId = a.ActivityId,
                        HoursWorked = a.HoursWorked,
                        IsActive = true,
                        CreatedAtUtc = dto.CreatedAtUtc,
                        UpdatedAtUtc = dto.UpdatedAtUtc
                    }).ToList(),
                    Supplies = c.Supplies.Select(s => new RecoveryWorkSupply
                    {
                        Id = s.Id,
                        RecoveryWorkReportCategoryId = c.Id,
                        SupplyId = s.SupplyId,
                        Quantity = s.Quantity,
                        IsActive = true,
                        CreatedAtUtc = dto.CreatedAtUtc,
                        UpdatedAtUtc = dto.UpdatedAtUtc
                    }).ToList()
                }).ToList()
            };

            _db.RecoveryWorkReports.Add(entity);

            // Todo el agregado (reporte + categorías + actividades +
            // insumos) se guarda en una sola transacción implícita de
            // SaveChangesAsync — o se guarda todo, o no se guarda nada.
            await _db.SaveChangesAsync();

            return Ok(dto);
        }

        private static RecoveryWorkReportSyncDto ToDto(RecoveryWorkReport report)
        {
            return new RecoveryWorkReportSyncDto
            {
                Id = report.Id,
                UserId = report.UserId,
                WorkDate = report.WorkDate,
                ShiftName = report.ShiftName,
                Notes = report.Notes,
                IsActive = report.IsActive,
                CreatedAtUtc = report.CreatedAtUtc,
                UpdatedAtUtc = report.UpdatedAtUtc,
                Categories = report.Categories.Select(c => new RecoveryWorkReportCategorySyncDto
                {
                    Id = c.Id,
                    WorkType = c.WorkType,
                    PlantId = c.PlantId,
                    BarTypeId = c.BarTypeId,
                    BarsWorkedCount = c.BarsWorkedCount,
                    ExportLabel = c.ExportLabel,
                    Activities = c.Activities.Select(a => new RecoveryWorkActivitySyncDto
                    {
                        Id = a.Id,
                        ActivityId = a.ActivityId,
                        HoursWorked = a.HoursWorked
                    }).ToList(),
                    Supplies = c.Supplies.Select(s => new RecoveryWorkSupplySyncDto
                    {
                        Id = s.Id,
                        SupplyId = s.SupplyId,
                        Quantity = s.Quantity
                    }).ToList()
                }).ToList()
            };
        }
    }
}