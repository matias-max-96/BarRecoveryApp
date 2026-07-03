using System.Text;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class BarImportService : IBarImportService
    {
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;

        public BarImportService(
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            ICurrentUserService currentUserService)
        {
            _barRepository = barRepository
                ?? throw new ArgumentNullException(nameof(barRepository));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<BarImportResultDto> ImportFromCsvStreamAsync(
            Stream stream,
            string fileName)
        {
            var result = new BarImportResultDto
            {
                FileName = fileName
            };

            if (!_currentUserService.IsAuthenticated)
            {
                result.Errors.Add(new BarImportErrorDto
                {
                    RowNumber = 0,
                    Message = "No existe una sesión activa."
                });

                return result;
            }

            if (!_currentUserService.HasPermission("BAR_MANAGE"))
            {
                result.Errors.Add(new BarImportErrorDto
                {
                    RowNumber = 0,
                    Message = "El usuario no tiene permiso para importar barras."
                });

                return result;
            }

            if (stream is null || !stream.CanRead)
            {
                result.Errors.Add(new BarImportErrorDto
                {
                    RowNumber = 0,
                    Message = "El archivo no se pudo leer."
                });

                return result;
            }

            var rows = await ReadCsvRowsAsync(stream, result);

            if (result.Errors.Count > 0)
                return result;

            await ImportRowsAsync(rows, result);

            return result;
        }

        private static async Task<List<BarImportRowDto>> ReadCsvRowsAsync(
            Stream stream,
            BarImportResultDto result)
        {
            var rows = new List<BarImportRowDto>();

            using var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: false);

            var allLines = new List<string>();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (!string.IsNullOrWhiteSpace(line))
                    allLines.Add(line);
            }

            if (allLines.Count == 0)
            {
                result.Errors.Add(new BarImportErrorDto
                {
                    RowNumber = 0,
                    Message = "El archivo CSV está vacío."
                });

                return rows;
            }

            var headerLine = allLines[0];
            var separator = DetectSeparator(headerLine);

            var headers = SplitLine(headerLine, separator)
                .Select(x => NormalizeHeader(x))
                .ToList();

            var barNumberIndex = FindHeaderIndex(headers,
                "BARNUMBER",
                "BAR_NUMBER",
                "NUMEROBARRA",
                "NUMERO_BARRA",
                "BARRA");

            var plantCodeIndex = FindHeaderIndex(headers,
                "PLANTCODE",
                "PLANT_CODE",
                "CODIGOPLANTA",
                "CODIGO_PLANTA",
                "PLANTA",
                "PLANT");

            var barTypeCodeIndex = FindHeaderIndex(headers,
                "BARTYPECODE",
                "BAR_TYPE_CODE",
                "CODIGOTIPOBARRA",
                "CODIGO_TIPO_BARRA",
                "TIPOBARRA",
                "TIPO_BARRA",
                "BARTYPE");

            if (barNumberIndex < 0 || plantCodeIndex < 0 || barTypeCodeIndex < 0)
            {
                result.Errors.Add(new BarImportErrorDto
                {
                    RowNumber = 1,
                    Message = "El encabezado debe contener BarNumber, PlantCode y BarTypeCode."
                });

                return rows;
            }

            for (int i = 1; i < allLines.Count; i++)
            {
                var rowNumber = i + 1;
                var line = allLines[i];

                var values = SplitLine(line, separator);

                result.TotalRowsRead++;

                if (values.Count <= Math.Max(barNumberIndex, Math.Max(plantCodeIndex, barTypeCodeIndex)))
                {
                    result.Errors.Add(new BarImportErrorDto
                    {
                        RowNumber = rowNumber,
                        Message = "La fila no tiene la cantidad mínima de columnas."
                    });

                    continue;
                }

                rows.Add(new BarImportRowDto
                {
                    RowNumber = rowNumber,
                    BarNumber = values[barNumberIndex].Trim(),
                    PlantCode = values[plantCodeIndex].Trim(),
                    BarTypeCode = values[barTypeCodeIndex].Trim()
                });
            }

            return rows;
        }

        private async Task ImportRowsAsync(
            List<BarImportRowDto> rows,
            BarImportResultDto result)
        {
            var plants = await _plantRepository.GetAllAsync();
            var barTypes = await _barTypeRepository.GetAllAsync();
            var existingBars = await _barRepository.GetAllAsync();

            var plantsByCode = plants
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .ToDictionary(
                    x => x.Code.Trim().ToUpperInvariant(),
                    x => x);

            var barTypesByCode = barTypes
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .ToDictionary(
                    x => x.Code.Trim().ToUpperInvariant(),
                    x => x);

            var existingBarNumbers = existingBars
                .Where(x => !string.IsNullOrWhiteSpace(x.BarNumber))
                .Select(x => x.BarNumber.Trim().ToUpperInvariant())
                .ToHashSet();

            var fileBarNumbers = new HashSet<string>();

            foreach (var row in rows)
            {
                var normalizedBarNumber = row.BarNumber.Trim().ToUpperInvariant();
                var normalizedPlantCode = row.PlantCode.Trim().ToUpperInvariant();
                var normalizedBarTypeCode = row.BarTypeCode.Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(normalizedBarNumber))
                {
                    AddError(result, row, "El número de barra es obligatorio.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(normalizedPlantCode))
                {
                    AddError(result, row, "El código de planta es obligatorio.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(normalizedBarTypeCode))
                {
                    AddError(result, row, "El código de tipo de barra es obligatorio.");
                    continue;
                }

                if (!fileBarNumbers.Add(normalizedBarNumber))
                {
                    result.SkippedCount++;

                    AddError(result, row, $"La barra '{normalizedBarNumber}' está duplicada dentro del archivo.");
                    continue;
                }

                if (existingBarNumbers.Contains(normalizedBarNumber))
                {
                    result.SkippedCount++;

                    AddError(result, row, $"La barra '{normalizedBarNumber}' ya existe en la base de datos.");
                    continue;
                }

                if (!plantsByCode.TryGetValue(normalizedPlantCode, out var plant))
                {
                    AddError(result, row, $"No existe una planta con código '{normalizedPlantCode}'.");
                    continue;
                }

                if (!plant.IsActive)
                {
                    AddError(result, row, $"La planta '{normalizedPlantCode}' está inactiva.");
                    continue;
                }

                if (!barTypesByCode.TryGetValue(normalizedBarTypeCode, out var barType))
                {
                    AddError(result, row, $"No existe un tipo de barra con código '{normalizedBarTypeCode}'.");
                    continue;
                }

                if (!barType.IsActive)
                {
                    AddError(result, row, $"El tipo de barra '{normalizedBarTypeCode}' está inactivo.");
                    continue;
                }

                var bar = new Bar
                {
                    Id = Guid.NewGuid().ToString(),
                    BarNumber = normalizedBarNumber,
                    PlantId = plant.Id,
                    BarTypeId = barType.Id,
                    CurrentStatus = BarStatus.Created,
                    RecoveryCount = 0,
                    IsDisposed = false,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _barRepository.InsertAsync(bar);

                existingBarNumbers.Add(normalizedBarNumber);
                result.CreatedCount++;
            }
        }

        private static void AddError(
            BarImportResultDto result,
            BarImportRowDto row,
            string message)
        {
            result.Errors.Add(new BarImportErrorDto
            {
                RowNumber = row.RowNumber,
                BarNumber = row.BarNumber,
                Message = message
            });
        }

        private static char DetectSeparator(string headerLine)
        {
            var commaCount = headerLine.Count(x => x == ',');
            var semicolonCount = headerLine.Count(x => x == ';');
            var tabCount = headerLine.Count(x => x == '\t');

            if (semicolonCount >= commaCount && semicolonCount >= tabCount)
                return ';';

            if (tabCount >= commaCount && tabCount >= semicolonCount)
                return '\t';

            return ',';
        }

        private static List<string> SplitLine(string line, char separator)
        {
            return line
                .Split(separator)
                .Select(x => x.Trim().Trim('"'))
                .ToList();
        }

        private static string NormalizeHeader(string value)
        {
            return value
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("-", "_")
                .ToUpperInvariant();
        }

        private static int FindHeaderIndex(
            List<string> headers,
            params string[] names)
        {
            foreach (var name in names)
            {
                var normalizedName = NormalizeHeader(name);

                var index = headers.FindIndex(x => x == normalizedName);

                if (index >= 0)
                    return index;
            }

            return -1;
        }
    }
}