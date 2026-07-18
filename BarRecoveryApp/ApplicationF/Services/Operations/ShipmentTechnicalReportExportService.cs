using ClosedXML.Excel;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class ShipmentTechnicalReportExportService : IShipmentTechnicalReportExportService
    {
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly IRepository<QualityInspection> _inspectionRepository;
        private readonly IRepository<QualityInspectionAttributeValue> _attributeValueRepository;
        private readonly IRepository<User> _userRepository;

        public ShipmentTechnicalReportExportService(
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            IRepository<QualityInspection> inspectionRepository,
            IRepository<QualityInspectionAttributeValue> attributeValueRepository,
            IRepository<User> userRepository)
        {
            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _inspectionRepository = inspectionRepository
                ?? throw new ArgumentNullException(nameof(inspectionRepository));

            _attributeValueRepository = attributeValueRepository
                ?? throw new ArgumentNullException(nameof(attributeValueRepository));

            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<ExportFileResultDto> ExportShipmentTechnicalReportAsync(
            Shipment shipment,
            List<Bar> shippedBars)
        {
            try
            {
                if (shipment is null)
                {
                    return new ExportFileResultDto
                    {
                        Success = false,
                        Message = "No existe envío para generar reporte técnico."
                    };
                }

                if (shippedBars is null || shippedBars.Count == 0)
                {
                    return new ExportFileResultDto
                    {
                        Success = false,
                        Message = "El envío no tiene barras para generar reporte técnico."
                    };
                }

                var plants = await _plantRepository.GetAllAsync();
                var barTypes = await _barTypeRepository.GetAllAsync();
                var inspections = await _inspectionRepository.GetAllAsync();
                var attributeValues = await _attributeValueRepository.GetAllAsync();
                var users = await _userRepository.GetAllAsync();

                var rows = new List<TechnicalReportRow>();

                foreach (var bar in shippedBars)
                {
                    var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                    var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                    var latestInspection = inspections
                        .Where(x => x.BarId == bar.Id && x.IsActive)
                        .OrderByDescending(x => x.InspectionAtUtc)
                        .FirstOrDefault();

                    var inspector = latestInspection is null
                        ? null
                        : users.FirstOrDefault(x => x.Id == latestInspection.InspectorUserId);

                    var values = latestInspection is null
                        ? new List<QualityInspectionAttributeValue>()
                        : attributeValues
                            .Where(x => x.QualityInspectionId == latestInspection.Id && x.IsActive)
                            .OrderBy(x => x.AttributeCode)
                            .ToList();

                    rows.Add(new TechnicalReportRow
                    {
                        Bar = bar,
                        Plant = plant,
                        BarType = barType,
                        Inspection = latestInspection,
                        Inspector = inspector,
                        AttributeValues = values
                    });
                }

                var attributeCodes = rows
                    .SelectMany(x => x.AttributeValues)
                    .Select(x => x.AttributeCode)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var exportDirectory = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "Exports");

                Directory.CreateDirectory(exportDirectory);

                var safeTransferOrder = NormalizeFileNamePart(shipment.TransferOrder);
                var fileName = $"registro_barras_envio_{safeTransferOrder}_{DateTime.Now:yyyyMMdd}.xlsx";
                var filePath = Path.Combine(exportDirectory, fileName);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Registro técnico");

                WriteHeader(worksheet, attributeCodes);
                WriteRows(worksheet, shipment, rows, attributeCodes);

                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(filePath);

                var fileInfo = new FileInfo(filePath);

                if (!fileInfo.Exists || fileInfo.Length == 0)
                {
                    return new ExportFileResultDto
                    {
                        Success = false,
                        Message = "No fue posible generar el reporte técnico del envío."
                    };
                }

                return new ExportFileResultDto
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = fileName,
                    Message = $"Reporte técnico generado correctamente: {fileName}"
                };
            }
            catch (Exception ex)
            {
                return new ExportFileResultDto
                {
                    Success = false,
                    Message = $"Error generando reporte técnico del envío: {ex.Message}"
                };
            }
        }

        private static void WriteHeader(
            IXLWorksheet worksheet,
            List<string> attributeCodes)
        {
            var column = 1;

            worksheet.Cell(1, column++).Value = "Orden traslado";
            worksheet.Cell(1, column++).Value = "Fecha envío";
            worksheet.Cell(1, column++).Value = "OperationalKey";
            worksheet.Cell(1, column++).Value = "N° barra";
            worksheet.Cell(1, column++).Value = "Planta";
            worksheet.Cell(1, column++).Value = "Tipo barra";
            worksheet.Cell(1, column++).Value = "Estado";
            worksheet.Cell(1, column++).Value = "Recuperaciones";
            worksheet.Cell(1, column++).Value = "Fecha inspección";
            worksheet.Cell(1, column++).Value = "Inspector";

            foreach (var code in attributeCodes)
            {
                worksheet.Cell(1, column++).Value = code;
                worksheet.Cell(1, column++).Value = $"{code} tolerancia";
                worksheet.Cell(1, column++).Value = $"{code} estado";
            }

            var headerRange = worksheet.Range(1, 1, 1, column - 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        private static void WriteRows(
            IXLWorksheet worksheet,
            Shipment shipment,
            List<TechnicalReportRow> rows,
            List<string> attributeCodes)
        {
            var rowIndex = 2;

            foreach (var row in rows)
            {
                var column = 1;

                var operationalKey = BuildOperationalKey(
                    row.Bar,
                    row.Plant,
                    row.BarType);

                worksheet.Cell(rowIndex, column++).Value = shipment.TransferOrder;
                worksheet.Cell(rowIndex, column++).Value = shipment.ShippedAtUtc.ToString("dd-MM-yyyy HH:mm");
                worksheet.Cell(rowIndex, column++).Value = operationalKey;
                worksheet.Cell(rowIndex, column++).Value = row.Bar.BarNumber;
                worksheet.Cell(rowIndex, column++).Value = row.Plant?.Name ?? "Planta no encontrada";
                worksheet.Cell(rowIndex, column++).Value = row.BarType?.Name ?? "Tipo no encontrado";
                worksheet.Cell(rowIndex, column++).Value = row.Bar.CurrentStatus.ToString();
                worksheet.Cell(rowIndex, column++).Value = row.Bar.RecoveryCount;
                worksheet.Cell(rowIndex, column++).Value = row.Inspection?.InspectionAtUtc.ToString("dd-MM-yyyy HH:mm") ?? string.Empty;
                worksheet.Cell(rowIndex, column++).Value = row.Inspector?.DisplayName ?? string.Empty;

                foreach (var code in attributeCodes)
                {
                    var value = row.AttributeValues
                        .FirstOrDefault(x => x.AttributeCode == code);

                    var valueCell = worksheet.Cell(rowIndex, column++);
                    var toleranceCell = worksheet.Cell(rowIndex, column++);
                    var statusCell = worksheet.Cell(rowIndex, column++);

                    if (value is null)
                    {
                        valueCell.Value = "Sin dato";
                        toleranceCell.Value = string.Empty;
                        statusCell.Value = "Sin dato";

                        valueCell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        continue;
                    }

                    valueCell.Value = GetAttributeValueText(value);
                    toleranceCell.Value = value.ToleranceTextAtInspection ?? string.Empty;
                    statusCell.Value = GetAttributeStatusText(value);

                    if (value.WasMeasured == false)
                    {
                        valueCell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                    else if (value.IsOutOfRange == true)
                    {
                        valueCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                    }
                    else if (value.IsOutOfRange == false)
                    {
                        valueCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    }
                }

                rowIndex++;
            }
        }

        private static string GetAttributeValueText(
            QualityInspectionAttributeValue value)
        {
            if (!value.WasMeasured)
                return "No medido";

            if (value.DataType == AttributeDataType.Boolean)
            {
                if (!value.ValueBool.HasValue)
                    return "Sin resultado";

                return value.ValueBool.Value ? "Sí" : "No";
            }

            if (value.ValueNumber.HasValue)
                return value.ValueNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(value.ValueText))
                return value.ValueText;

            if (value.ValueDate.HasValue)
                return value.ValueDate.Value.ToString("dd-MM-yyyy");

            return string.Empty;
        }

        private static string GetAttributeStatusText(
            QualityInspectionAttributeValue value)
        {
            if (!value.WasMeasured)
                return "No medido";

            if (value.IsOutOfRange == true)
                return "Fuera de rango";

            if (value.IsOutOfRange == false)
                return "Dentro de rango";

            if (value.DataType == AttributeDataType.Boolean)
                return "Realizado";

            return string.Empty;
        }

        private static string BuildOperationalKey(
            Bar bar,
            Plant? plant,
            BarType? barType)
        {
            var plantCode = string.IsNullOrWhiteSpace(plant?.Code)
                ? plant?.Name ?? "PLANTA"
                : plant.Code;

            var barTypeCode = string.IsNullOrWhiteSpace(barType?.Code)
                ? barType?.Name ?? "TIPO"
                : barType.Code;

            return $"{NormalizeKeyPart(plantCode)}-{NormalizeKeyPart(barTypeCode)}-{NormalizeKeyPart(bar.BarNumber)}";
        }

        private static string NormalizeKeyPart(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("\"", string.Empty);
        }

        private static string NormalizeFileNamePart(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "SIN_OT";

            return value
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace("\"", string.Empty);
        }

        private sealed class TechnicalReportRow
        {
            public Bar Bar { get; set; } = default!;

            public Plant? Plant { get; set; }

            public BarType? BarType { get; set; }

            public QualityInspection? Inspection { get; set; }

            public User? Inspector { get; set; }

            public List<QualityInspectionAttributeValue> AttributeValues { get; set; } = new();
        }
    }
}