using System.Text;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class ReportExportService : IReportExportService
    {
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;

        public ReportExportService(
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
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                var builder = new StringBuilder();

                /*
                 * Usamos punto y coma porque Excel en configuración regional española/chilena
                 * suele abrir mejor CSV con separador ';'.
                 */
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