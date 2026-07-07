using System.Text;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using ClosedXML.Excel;
using BarRecoveryApp.Models.Security;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class ReportExportService : IReportExportService
    {
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRepository<RecoveryWorkReport> _recoveryReportRepository;
        private readonly IRepository<RecoveryWorkActivity> _recoveryActivityRepository;
        private readonly IRepository<RecoveryWorkSupply> _recoverySupplyRepository;
        private readonly IRepository<Activity> _activityRepository;
        private readonly IRepository<Supply> _supplyRepository;
        private readonly IRepository<User> _userRepository;

        public ReportExportService(
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            IRepository<RecoveryWorkReport> recoveryReportRepository,
            IRepository<RecoveryWorkActivity> recoveryActivityRepository,
            IRepository<RecoveryWorkSupply> recoverySupplyRepository,
            IRepository<ActivityModel> activityRepository,
            IRepository<Supply> supplyRepository,
            IRepository<User> userRepository,
            ICurrentUserService currentUserService)
        {
            _barRepository = barRepository
                ?? throw new ArgumentNullException(nameof(barRepository));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _recoveryReportRepository = recoveryReportRepository
                ?? throw new ArgumentNullException(nameof(recoveryReportRepository));

            _recoveryActivityRepository = recoveryActivityRepository
                ?? throw new ArgumentNullException(nameof(recoveryActivityRepository));

            _recoverySupplyRepository = recoverySupplyRepository
                ?? throw new ArgumentNullException(nameof(recoverySupplyRepository));

            _activityRepository = activityRepository
                ?? throw new ArgumentNullException(nameof(activityRepository));

            _supplyRepository = supplyRepository
                ?? throw new ArgumentNullException(nameof(supplyRepository));

            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<ExportFileResultDto> ExportBarsCsvAsync()
        {
            if (!_currentUserService.IsAuthenticated)
            {
                return new ExportFileResultDto
                {
                    Success = false,
                    Message = "No existe una sesión activa."
                };
            }

            if (!_currentUserService.HasPermission("EXPORT_EXCEL"))
            {
                return new ExportFileResultDto
                {
                    Success = false,
                    Message = "El usuario no tiene permiso para exportar reportes."
                };
            }

            try
            {
                var bars = await _barRepository.GetAllAsync();
                var plants = await _plantRepository.GetAllAsync();
                var barTypes = await _barTypeRepository.GetAllAsync();

                var fileName = $"reporte_barras_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                var exportDirectory = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "Exports");

                Directory.CreateDirectory(exportDirectory);

                var filePath = Path.Combine(exportDirectory, fileName);

                var builder = new StringBuilder();

                builder.AppendLine(
                    "BarNumber;Plant;PlantCode;BarType;BarTypeCode;RecoveryCount;Status;IsDisposed;IsActive;CreatedAt;UpdatedAt");

                foreach (var bar in bars.OrderBy(x => x.BarNumber))
                {
                    var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                    var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                    var line = string.Join(";",
                        EscapeCsv(bar.BarNumber),
                        EscapeCsv(plant?.Name ?? "Planta no encontrada"),
                        EscapeCsv(plant?.Code ?? string.Empty),
                        EscapeCsv(barType?.Name ?? "Tipo no encontrado"),
                        EscapeCsv(barType?.Code ?? string.Empty),
                        bar.RecoveryCount.ToString(),
                        EscapeCsv(GetStatusText(bar.CurrentStatus, bar.IsDisposed)),
                        bar.IsDisposed ? "Sí" : "No",
                        bar.IsActive ? "Sí" : "No",
                        bar.CreatedAtUtc.ToString("dd-MM-yyyy HH:mm:ss"),
                        bar.UpdatedAtUtc.ToString("dd-MM-yyyy HH:mm:ss")
                    );

                    builder.AppendLine(line);
                }

                await File.WriteAllTextAsync(
                    filePath,
                    builder.ToString(),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                return new ExportFileResultDto
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = fileName,
                    Message = $"Reporte generado correctamente: {fileName}"
                };
            }
            catch (Exception ex)
            {
                return new ExportFileResultDto
                {
                    Success = false,
                    Message = $"Error exportando reporte de barras: {ex.Message}"
                };
            }
        }
        public async Task<ExportFileResultDto> ExportProductionWorkbookAsync(
    DateTime fromDate,
    DateTime toDate)
        {
            if (!_currentUserService.IsAuthenticated)
            {
                return new ExportFileResultDto
                {
                    Success = false,
                    Message = "No existe una sesión activa."
                };
            }

            if (!_currentUserService.HasPermission("EXPORT_EXCEL"))
            {
                return new ExportFileResultDto
                {
                    Success = false,
                    Message = "El usuario no tiene permiso para exportar reportes."
                };
            }

            if (toDate.Date < fromDate.Date)
            {
                return new ExportFileResultDto
                {
                    Success = false,
                    Message = "La fecha hasta no puede ser menor que la fecha desde."
                };
            }

            try
            {
                var reports = await _recoveryReportRepository.GetAllAsync();
                var reportActivities = await _recoveryActivityRepository.GetAllAsync();
                var reportSupplies = await _recoverySupplyRepository.GetAllAsync();
                var activities = await _activityRepository.GetAllAsync();
                var supplies = await _supplyRepository.GetAllAsync();
                var users = await _userRepository.GetAllAsync();

                var filteredReports = reports
                    .Where(x => x.IsActive &&
                                x.WorkDate.Date >= fromDate.Date &&
                                x.WorkDate.Date <= toDate.Date)
                    .OrderBy(x => x.WorkDate)
                    .ToList();

                if (filteredReports.Count == 0)
                {
                    return new ExportFileResultDto
                    {
                        Success = false,
                        Message = "No existen registros de recuperación en el rango seleccionado."
                    };
                }

                var fileName = $"reporte_produccion_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                using var workbook = new XLWorkbook();

                var reportsByUser = filteredReports
                    .GroupBy(x => x.UserId)
                    .ToList();

                var dates = Enumerable
                    .Range(0, (toDate.Date - fromDate.Date).Days + 1)
                    .Select(offset => fromDate.Date.AddDays(offset))
                    .ToList();

                foreach (var userGroup in reportsByUser)
                {
                    var user = users.FirstOrDefault(x => x.Id == userGroup.Key);
                    var operatorName = user?.DisplayName ?? "Operador";

                    var sheetName = SanitizeSheetName(operatorName);
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    BuildProductionSheet(
                        worksheet,
                        operatorName,
                        dates,
                        userGroup.ToList(),
                        reportActivities,
                        reportSupplies,
                        activities,
                        supplies);
                }

                workbook.SaveAs(filePath);

                return new ExportFileResultDto
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = fileName,
                    Message = $"Reporte generado correctamente: {fileName}"
                };
            }
            catch (Exception ex)
            {
                return new ExportFileResultDto
                {
                    Success = false,
                    Message = $"Error exportando reporte de producción: {ex.Message}"
                };
            }
        }
        private static void BuildProductionSheet(
    IXLWorksheet worksheet,
    string operatorName,
    List<DateTime> dates,
    List<RecoveryWorkReport> reports,
    List<RecoveryWorkActivity> reportActivities,
    List<RecoveryWorkSupply> reportSupplies,
    List<ActivityModel> activities,
    List<Supply> supplies)
        {
            worksheet.Cell(1, 1).Value = operatorName;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;

            worksheet.Cell(2, 1).Value = "Actividad / Insumo";
            worksheet.Cell(2, 2).Value = "Unidad";

            for (int i = 0; i < dates.Count; i++)
            {
                var column = i + 3;
                worksheet.Cell(2, column).Value = dates[i];
                worksheet.Cell(2, column).Style.DateFormat.Format = "dd-MM-yyyy";
            }

            var rowDefinitions = GetProductionRows();

            var currentRow = 3;

            foreach (var row in rowDefinitions)
            {
                if (row.IsSection)
                {
                    worksheet.Cell(currentRow, 1).Value = row.Label;
                    worksheet.Range(currentRow, 1, currentRow, dates.Count + 2).Merge();
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF2FF");
                    currentRow++;
                    continue;
                }

                worksheet.Cell(currentRow, 1).Value = row.Label;
                worksheet.Cell(currentRow, 2).Value = row.Unit;

                for (int i = 0; i < dates.Count; i++)
                {
                    var date = dates[i];
                    var column = i + 3;

                    var value = GetValueForRowAndDate(
                        row,
                        date,
                        reports,
                        reportActivities,
                        reportSupplies,
                        activities,
                        supplies);

                    if (value.HasValue)
                    {
                        worksheet.Cell(currentRow, column).Value = value.Value;
                        worksheet.Cell(currentRow, column).Style.NumberFormat.Format = "#,##0.##";
                    }
                }

                currentRow++;
            }

            var usedRange = worksheet.Range(1, 1, currentRow - 1, dates.Count + 2);

            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

            worksheet.Row(2).Style.Font.Bold = true;
            worksheet.Row(2).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

            worksheet.Column(1).Width = 32;
            worksheet.Column(2).Width = 12;

            for (int i = 0; i < dates.Count; i++)
            {
                worksheet.Column(i + 3).Width = 12;
            }

            worksheet.SheetView.FreezeRows(2);
            worksheet.SheetView.FreezeColumns(2);
        }

        private static double? GetValueForRowAndDate(
            ProductionExportRow row,
            DateTime date,
            List<RecoveryWorkReport> reports,
            List<RecoveryWorkActivity> reportActivities,
            List<RecoveryWorkSupply> reportSupplies,
            List<ActivityModel> activities,
            List<Supply> supplies)
        {
            var dayReports = reports
                .Where(x => x.WorkDate.Date == date.Date)
                .ToList();

            if (dayReports.Count == 0)
                return null;

            var dayReportIds = dayReports
                .Select(x => x.Id)
                .ToHashSet();

            if (row.Kind == ProductionExportRowKind.Activity)
            {
                var matchingActivityIds = activities
                    .Where(x => NormalizeName(x.Name) == NormalizeName(row.Label) ||
                                NormalizeName(x.Code) == NormalizeName(row.Label))
                    .Select(x => x.Id)
                    .ToHashSet();

                var total = reportActivities
                    .Where(x => dayReportIds.Contains(x.RecoveryWorkReportId) &&
                                matchingActivityIds.Contains(x.ActivityId) &&
                                x.IsActive)
                    .Sum(x => x.HoursWorked);

                return total == 0 ? null : total;
            }

            if (row.Kind == ProductionExportRowKind.Supply)
            {
                var matchingSupplyIds = supplies
                    .Where(x => NormalizeName(x.Name) == NormalizeName(row.Label) ||
                                NormalizeName(x.Code) == NormalizeName(row.Label))
                    .Select(x => x.Id)
                    .ToHashSet();

                var total = reportSupplies
                    .Where(x => dayReportIds.Contains(x.RecoveryWorkReportId) &&
                                matchingSupplyIds.Contains(x.SupplyId) &&
                                x.IsActive)
                    .Sum(x => x.Quantity);

                return total == 0 ? null : total;
            }

            if (row.Kind == ProductionExportRowKind.BarsWorked)
            {
                /*
                 * Ojo: con el modelo actual, BarsWorkedCount no distingue MAPA / Santa Fe /
                 * Nueva Aldea / 90° / 60°. Por eso este valor solo se puede llenar de forma
                 * exacta si más adelante agregamos clasificación de sección al registro.
                 */
                return null;
            }

            return null;
        }

        private static List<ProductionExportRow> GetProductionRows()
        {
            var rows = new List<ProductionExportRow>();

            AddSection(rows, "Actividades de produccion");

            AddProductionBlock(rows, "Barras MAPA");
            AddProductionBlock(rows, "Barras Santa Fe");
            AddProductionBlock(rows, "Barras Nueva Aldea");

            AddCustomBlock(rows, "Recuperacion Barras 90°", "Rectificado Barras 90°");
            AddCustomBlock(rows, "Fabricacion Barras 90°", "Rectificado Barras 90°");
            AddCustomBlock(rows, "Fabricacion Barras 60°", "Rectificado Barras 60°");

            return rows;
        }

        private static void AddSection(List<ProductionExportRow> rows, string label)
        {
            rows.Add(new ProductionExportRow
            {
                Label = label,
                IsSection = true
            });
        }

        private static void AddProductionBlock(List<ProductionExportRow> rows, string section)
        {
            rows.Add(new ProductionExportRow
            {
                Label = $"Recuperacion {section}",
                Unit = "Unidad",
                Kind = ProductionExportRowKind.BarsWorked
            });

            rows.Add(new ProductionExportRow
            {
                Label = $"Rectificado {section}",
                Unit = "Unidad",
                Kind = ProductionExportRowKind.Activity
            });

            AddCommonConsumables(rows);
        }

        private static void AddCustomBlock(
            List<ProductionExportRow> rows,
            string mainLabel,
            string rectificadoLabel)
        {
            rows.Add(new ProductionExportRow
            {
                Label = mainLabel,
                Unit = "Unidad",
                Kind = ProductionExportRowKind.BarsWorked
            });

            rows.Add(new ProductionExportRow
            {
                Label = rectificadoLabel,
                Unit = "Unidad",
                Kind = ProductionExportRowKind.Activity
            });

            AddCommonConsumables(rows);
        }

        private static void AddCommonConsumables(List<ProductionExportRow> rows)
        {
            rows.Add(new ProductionExportRow
            {
                Label = "Horas",
                Unit = "Hora",
                Kind = ProductionExportRowKind.Activity
            });

            rows.Add(new ProductionExportRow
            {
                Label = "Soldadura Hilcord 600",
                Unit = "kg",
                Kind = ProductionExportRowKind.Supply
            });

            rows.Add(new ProductionExportRow
            {
                Label = "Soldadura Corodur 600G",
                Unit = "kg",
                Kind = ProductionExportRowKind.Supply
            });

            rows.Add(new ProductionExportRow
            {
                Label = "Gas mezcla",
                Unit = "PSI",
                Kind = ProductionExportRowKind.Supply
            });

            rows.Add(new ProductionExportRow
            {
                Label = "Cubitron 7\"",
                Unit = "unidad",
                Kind = ProductionExportRowKind.Supply
            });

            rows.Add(new ProductionExportRow
            {
                Label = "Cubitron 4 1/2\"",
                Unit = "unidad",
                Kind = ProductionExportRowKind.Supply
            });
        }

        private static string NormalizeName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace("\"", string.Empty)
                .Replace("°", string.Empty);
        }

        private static string SanitizeSheetName(string name)
        {
            var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };

            var cleanName = name;

            foreach (var invalidChar in invalidChars)
            {
                cleanName = cleanName.Replace(invalidChar, ' ');
            }

            cleanName = cleanName.Trim();

            if (string.IsNullOrWhiteSpace(cleanName))
                cleanName = "Operador";

            if (cleanName.Length > 31)
                cleanName = cleanName[..31];

            return cleanName;
        }

        private sealed class ProductionExportRow
        {
            public string Label { get; set; } = string.Empty;

            public string Unit { get; set; } = string.Empty;

            public bool IsSection { get; set; }

            public ProductionExportRowKind Kind { get; set; }
        }

        private enum ProductionExportRowKind
        {
            None = 0,
            BarsWorked = 1,
            Activity = 2,
            Supply = 3
        }
        private static string GetStatusText(
            BarStatus status,
            bool isDisposed)
        {
            if (isDisposed)
                return "Dada de baja";

            return status switch
            {
                BarStatus.Created => "Creada",
                BarStatus.InRecovery => "En recuperación",
                BarStatus.PendingQuality => "Pendiente calidad",
                BarStatus.Approved => "Aprobada",
                BarStatus.Rejected => "Rechazada",
                BarStatus.ReadyToShip => "Lista para envío",
                BarStatus.Shipped => "Enviada",
                BarStatus.Disposed => "Dada de baja",
                BarStatus.Returned => "Retornada / Disponible",
                _ => "Desconocido"
            };
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var escaped = value.Replace("\"", "\"\"");

            if (escaped.Contains(';') ||
                escaped.Contains('"') ||
                escaped.Contains('\n') ||
                escaped.Contains('\r'))
            {
                return $"\"{escaped}\"";
            }

            return escaped;
        }
    }
}