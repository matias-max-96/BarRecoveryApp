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
using BarRecoveryApp.ApplicationF.Services.Auditing;

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
        private readonly IRepository<RecoveryWorkReportCategory> _recoveryCategoryRepository;
        private readonly IRepository<ActivityModel> _activityRepository;
        private readonly IRepository<Supply> _supplyRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IAuditLogService _auditLogService;
        

        public ReportExportService(
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            IRepository<RecoveryWorkReport> recoveryReportRepository,
            IRepository<RecoveryWorkActivity> recoveryActivityRepository,
            IRepository<RecoveryWorkSupply> recoverySupplyRepository,
            IRepository<RecoveryWorkReportCategory> recoveryCategoryRepository,
            IRepository<ActivityModel> activityRepository,
            IRepository<Supply> supplyRepository,
            IRepository<User> userRepository,
            ICurrentUserService currentUserService,
            IAuditLogService auditLogService)
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

            _recoveryCategoryRepository = recoveryCategoryRepository
                ?? throw new ArgumentNullException(nameof(recoveryCategoryRepository));

            _activityRepository = activityRepository
                ?? throw new ArgumentNullException(nameof(activityRepository));

            _supplyRepository = supplyRepository
                ?? throw new ArgumentNullException(nameof(supplyRepository));

            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));

            _auditLogService = auditLogService
                ?? throw new ArgumentNullException(nameof(auditLogService));
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

                var fileName = $"reporte_barras_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

                var fileInfo = new FileInfo(filePath);

                await _auditLogService.WriteAsync(
                    AuditActionCodes.ReportExported,
                    "BarsCsvReport",
                    null,
                    $"se exporto reporte maestro de barras. Archivo generado: {fileName}.",
                    BuildReportExportMetadataJson(
                        "BarsCsvReport",
                        fileName,
                        filePath,
                        fileInfo.Length,
                        null,
                        null));

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
                var categories = await _recoveryCategoryRepository.GetAllAsync();
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

                var filteredReportIds = filteredReports
                    .Select(x => x.Id)
                    .ToHashSet();

                var filteredCategories = categories
                    .Where(x => x.IsActive &&
                                filteredReportIds.Contains(x.RecoveryWorkReportId))
                    .ToList();

                var filteredCategoryIds = filteredCategories
                    .Select(x => x.Id)
                    .ToHashSet();

                var filteredActivities = reportActivities
                    .Where(x => x.IsActive &&
                                filteredCategoryIds.Contains(x.RecoveryWorkReportCategoryId))
                    .ToList();

                var filteredSupplies = reportSupplies
                    .Where(x => x.IsActive &&
                                filteredCategoryIds.Contains(x.RecoveryWorkReportCategoryId))
                    .ToList();

                var dates = Enumerable
                    .Range(0, (toDate.Date - fromDate.Date).Days + 1)
                    .Select(offset => fromDate.Date.AddDays(offset))
                    .ToList();

                var fileName = $"reporte_produccion_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

                var exportDirectory = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "Exports");

                Directory.CreateDirectory(exportDirectory);

                var filePath = Path.Combine(exportDirectory, fileName);

                using var workbook = new XLWorkbook();

                var reportsByUser = filteredReports
                    .GroupBy(x => x.UserId)
                    .ToList();

                foreach (var userGroup in reportsByUser)
                {
                    var user = users.FirstOrDefault(x => x.Id == userGroup.Key);

                    var operatorName = user?.DisplayName ?? "Operador";

                    var sheetName = SanitizeSheetName(operatorName);

                    var worksheet = workbook.Worksheets.Add(sheetName);

                    var userReports = userGroup
                        .OrderBy(x => x.WorkDate)
                        .ToList();

                    var userReportIds = userReports
                        .Select(x => x.Id)
                        .ToHashSet();

                    var userCategories = filteredCategories
                        .Where(x => userReportIds.Contains(x.RecoveryWorkReportId))
                        .ToList();

                    var userCategoryIds = userCategories
                        .Select(x => x.Id)
                        .ToHashSet();

                    var userActivities = filteredActivities
                        .Where(x => userCategoryIds.Contains(x.RecoveryWorkReportCategoryId))
                        .ToList();

                    var userSupplies = filteredSupplies
                        .Where(x => userCategoryIds.Contains(x.RecoveryWorkReportCategoryId))
                        .ToList();

                    BuildProductionSheet(
                        worksheet,
                        operatorName,
                        dates,
                        userReports,
                        userCategories,
                        userActivities,
                        userSupplies,
                        activities,
                        supplies);
                }

                await using (var fileStream = File.Create(filePath))
                {
                    workbook.SaveAs(fileStream);
                }

                if (!File.Exists(filePath))
                {
                    return new ExportFileResultDto
                    {
                        Success = false,
                        Message = $"El archivo no fue creado en la ruta esperada: {filePath}"
                    };
                }

                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length == 0)
                {
                    return new ExportFileResultDto
                    {
                        Success = false,
                        Message = $"El archivo fue creado, pero está vacío: {filePath}"
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Archivo Excel exportado: {filePath}");
                System.Diagnostics.Debug.WriteLine($"Tamaño archivo: {fileInfo.Length} bytes");

                await _auditLogService.WriteAsync(
                    AuditActionCodes.ReportExported,
                    "ProductionReport",
                    null,
                    $"Se exportó reporte de producción desde {fromDate:dd-MM-yyyy} hasta {toDate:dd-MM-yyyy}. Archivo generado: {fileName}.",
                    BuildReportExportMetadataJson(
                        "ProductionReport",
                        fileName,
                        filePath,
                        fileInfo.Length,
                        fromDate,
                        toDate));

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
                                List<RecoveryWorkReportCategory> categories,
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

            worksheet.Cell(3, 1).Value = "Horas jornada";
            worksheet.Cell(3, 2).Value = "Hora";

            for (int i = 0; i < dates.Count; i++)
            {
                var column = i + 3;
                worksheet.Cell(3, column).Value = 7.5;
                worksheet.Cell(3, column).Style.NumberFormat.Format = "#,##0.##";
            }

            var currentRow = 5;

            worksheet.Cell(currentRow, 1).Value = "Actividades de produccion";
            worksheet.Range(currentRow, 1, currentRow, dates.Count + 2).Merge();
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF2FF");

            currentRow++;

            var categoryGroups = categories
                .OrderBy(x => x.ExportLabel)
                .GroupBy(x => x.ExportLabel)
                .ToList();

            foreach (var categoryGroup in categoryGroups)
            {
                var exportLabel = categoryGroup.Key;

                WriteBarsWorkedRow(
                    worksheet,
                    currentRow,
                    exportLabel,
                    dates,
                    reports,
                    categoryGroup.ToList());

                currentRow++;

                var activityIds = reportActivities
                    .Where(x => categoryGroup.Any(c => c.Id == x.RecoveryWorkReportCategoryId))
                    .Select(x => x.ActivityId)
                    .Distinct()
                    .ToList();

                foreach (var activityId in activityIds)
                {
                    var activity = activities.FirstOrDefault(x => x.Id == activityId);

                    if (activity is null)
                        continue;

                    WriteActivityRow(
                        worksheet,
                        currentRow,
                        activity,
                        exportLabel,
                        dates,
                        reports,
                        categoryGroup.ToList(),
                        reportActivities);

                    currentRow++;
                }

                var supplyIds = reportSupplies
                    .Where(x => categoryGroup.Any(c => c.Id == x.RecoveryWorkReportCategoryId))
                    .Select(x => x.SupplyId)
                    .Distinct()
                    .ToList();

                foreach (var supplyId in supplyIds)
                {
                    var supply = supplies.FirstOrDefault(x => x.Id == supplyId);

                    if (supply is null)
                        continue;

                    WriteSupplyRow(
                        worksheet,
                        currentRow,
                        supply,
                        exportLabel,
                        dates,
                        reports,
                        categoryGroup.ToList(),
                        reportSupplies);

                    currentRow++;
                }

                currentRow++;
            }

            var usedRange = worksheet.Range(1, 1, Math.Max(currentRow - 1, 5), dates.Count + 2);

            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

            worksheet.Row(2).Style.Font.Bold = true;
            worksheet.Row(2).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

            worksheet.Column(1).Width = 36;
            worksheet.Column(2).Width = 12;

            for (int i = 0; i < dates.Count; i++)
            {
                worksheet.Column(i + 3).Width = 12;
            }

            worksheet.SheetView.FreezeRows(2);
            worksheet.SheetView.FreezeColumns(2);
        }
        private static void WriteBarsWorkedRow(
                                IXLWorksheet worksheet,
                                int row,
                                string exportLabel,
                                List<DateTime> dates,
                                List<RecoveryWorkReport> reports,
                                List<RecoveryWorkReportCategory> categories)
        {
            worksheet.Cell(row, 1).Value = exportLabel;
            worksheet.Cell(row, 2).Value = "Unidad";

            worksheet.Cell(row, 1).Style.Font.Bold = true;

            for (int i = 0; i < dates.Count; i++)
            {
                var date = dates[i];
                var column = i + 3;

                var reportIds = reports
                    .Where(x => x.WorkDate.Date == date.Date)
                    .Select(x => x.Id)
                    .ToHashSet();

                var total = categories
                    .Where(x => reportIds.Contains(x.RecoveryWorkReportId))
                    .Sum(x => x.BarsWorkedCount);

                if (total > 0)
                {
                    worksheet.Cell(row, column).Value = total;
                    worksheet.Cell(row, column).Style.NumberFormat.Format = "#,##0";
                }
            }
        }
        private static void WriteActivityRow(
                                IXLWorksheet worksheet,
                                int row,
                                ActivityModel activity,
                                string exportLabel,
                                List<DateTime> dates,
                                List<RecoveryWorkReport> reports,
                                List<RecoveryWorkReportCategory> categories,
                                List<RecoveryWorkActivity> reportActivities)
        {
            worksheet.Cell(row, 1).Value = activity.Name;
            worksheet.Cell(row, 2).Value = "Hora";

            for (int i = 0; i < dates.Count; i++)
            {
                var date = dates[i];
                var column = i + 3;

                var reportIds = reports
                    .Where(x => x.WorkDate.Date == date.Date)
                    .Select(x => x.Id)
                    .ToHashSet();

                var categoryIds = categories
                    .Where(x => reportIds.Contains(x.RecoveryWorkReportId) &&
                                x.ExportLabel == exportLabel)
                    .Select(x => x.Id)
                    .ToHashSet();

                var total = reportActivities
                    .Where(x => categoryIds.Contains(x.RecoveryWorkReportCategoryId) &&
                                x.ActivityId == activity.Id)
                    .Sum(x => x.HoursWorked);

                if (total > 0)
                {
                    worksheet.Cell(row, column).Value = total;
                    worksheet.Cell(row, column).Style.NumberFormat.Format = "#,##0.##";
                }
            }
        }
        private static void WriteSupplyRow(
                                IXLWorksheet worksheet,
                                int row,
                                Supply supply,
                                string exportLabel,
                                List<DateTime> dates,
                                List<RecoveryWorkReport> reports,
                                List<RecoveryWorkReportCategory> categories,
                                List<RecoveryWorkSupply> reportSupplies)
        {
            worksheet.Cell(row, 1).Value = supply.Name;
            worksheet.Cell(row, 2).Value = supply.Unit;

            for (int i = 0; i < dates.Count; i++)
            {
                var date = dates[i];
                var column = i + 3;

                var reportIds = reports
                    .Where(x => x.WorkDate.Date == date.Date)
                    .Select(x => x.Id)
                    .ToHashSet();

                var categoryIds = categories
                    .Where(x => reportIds.Contains(x.RecoveryWorkReportId) &&
                                x.ExportLabel == exportLabel)
                    .Select(x => x.Id)
                    .ToHashSet();

                var total = reportSupplies
                    .Where(x => categoryIds.Contains(x.RecoveryWorkReportCategoryId) &&
                                x.SupplyId == supply.Id)
                    .Sum(x => x.Quantity);

                if (total > 0)
                {
                    worksheet.Cell(row, column).Value = total;
                    worksheet.Cell(row, column).Style.NumberFormat.Format = "#,##0.##";
                }
            }
        }
        private static double? GetValueForRowAndDate(
                                    ProductionExportRow row,
                                    DateTime date,
                                    List<RecoveryWorkReport> reports,
                                    List<RecoveryWorkActivity> reportActivities,
                                    List<RecoveryWorkSupply> reportSupplies,
                                    List<RecoveryWorkReportCategory> categories,
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

            var dayCategoryIds = categories
                    .Where(x => dayReportIds.Contains(x.RecoveryWorkReportId) && x.IsActive)
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
                    .Where(x => dayReportIds.Contains(x.RecoveryWorkReportCategoryId) &&
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
                    .Where(x => dayReportIds.Contains(x.RecoveryWorkReportCategoryId) &&
                                matchingSupplyIds.Contains(x.SupplyId) &&
                                x.IsActive)
                    .Sum(x => x.Quantity);

                return total == 0 ? null : total;
            }

            if (row.Kind == ProductionExportRowKind.BarsWorked)
            {
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
        private static string BuildReportExportMetadataJson(
                                string reportType,
                                string fileName,
                                string filePath,
                                long fileSizeBytes,
                                DateTime? fromDate,
                                DateTime? toDate)
        {
            var safeReportType = string.IsNullOrWhiteSpace(reportType)
                ? string.Empty
                : reportType.Trim().Replace("\"", "'");

            var safeFileName = string.IsNullOrWhiteSpace(fileName)
                ? string.Empty
                : fileName.Trim().Replace("\"", "'");

            var safeFilePath = string.IsNullOrWhiteSpace(filePath)
                ? string.Empty
                : filePath.Trim().Replace("\"", "'");

            var fromDateValue = fromDate.HasValue
                ? fromDate.Value.ToString("yyyy-MM-dd")
                : string.Empty;

            var toDateValue = toDate.HasValue
                ? toDate.Value.ToString("yyyy-MM-dd")
                : string.Empty;

            return
                "{" +
                $"\"ReportType\":\"{safeReportType}\"," +
                $"\"FileName\":\"{safeFileName}\"," +
                $"\"FilePath\":\"{safeFilePath}\"," +
                $"\"FileSizeBytes\":{fileSizeBytes}," +
                $"\"FromDate\":\"{fromDateValue}\"," +
                $"\"ToDate\":\"{toDateValue}\"" +
                "}";
        }
    }
}