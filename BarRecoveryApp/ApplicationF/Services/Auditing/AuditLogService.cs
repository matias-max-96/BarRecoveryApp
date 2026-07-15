using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Auditing
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ICurrentUserService _currentUserService;

        private const string AuditDirectoryName = "AuditLogs";
        private const string AuditFileName = "audit_log.csv";

        public AuditLogService(
            ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task WriteAsync(
            string actionCode,
            string entityName,
            string? entityId,
            string description,
            string? metadataJson = null)
        {
            try
            {
                var session = _currentUserService.CurrentSession;

                var occurredAt = DateTime.Now;

                var userId = session?.UserId ?? string.Empty;
                var roleCode = session?.RoleCode ?? string.Empty;

                var userDisplayName = GetUserDisplayNameFromSession(session);

                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid().ToString(),

                    UserId = userId,
                    UserDisplayName = userDisplayName,
                    RoleCode = roleCode,

                    ActionCode = string.IsNullOrWhiteSpace(actionCode)
                        ? string.Empty
                        : actionCode.Trim(),

                    EntityName = string.IsNullOrWhiteSpace(entityName)
                        ? string.Empty
                        : entityName.Trim(),

                    EntityId = string.IsNullOrWhiteSpace(entityId)
                        ? null
                        : entityId.Trim(),

                    Description = string.IsNullOrWhiteSpace(description)
                        ? string.Empty
                        : description.Trim(),

                    MetadataJson = string.IsNullOrWhiteSpace(metadataJson)
                        ? null
                        : metadataJson.Trim(),

                    OldValuesJson = null,
                    NewValuesJson = null,

                    OccurredAtUtc = occurredAt,
                    DeviceId = DeviceInfo.Current.Name ?? string.Empty,
                    SyncStatus = SyncStatus.Pending,

                    IsActive = true,
                    CreatedAtUtc = occurredAt,
                    UpdatedAtUtc = occurredAt
                };

                await AppendAuditLogToCsvAsync(auditLog);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("====================================");
                System.Diagnostics.Debug.WriteLine("ERROR REGISTRANDO AUDITORÍA CSV");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine("====================================");
            }
        }

        public async Task<List<AuditLog>> GetRecentAsync(int maxResults)
        {
            try
            {
                var filePath = GetAuditFilePath();

                if (!File.Exists(filePath))
                    return new List<AuditLog>();

                var lines = await File.ReadAllLinesAsync(filePath);

                if (lines.Length <= 1)
                    return new List<AuditLog>();

                var result = new List<AuditLog>();

                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var columns = SplitCsvLine(line);

                    if (columns.Count < 8)
                        continue;

                    var parsedDate = DateTime.TryParse(
                        $"{columns[3]} {columns[4]}",
                        out var occurredAt)
                        ? occurredAt
                        : DateTime.Now;

                    result.Add(new AuditLog
                    {
                        UserDisplayName = columns[0],
                        UserId = columns[1],
                        RoleCode = columns[2],
                        OccurredAtUtc = parsedDate,
                        ActionCode = columns[5],
                        EntityName = columns[6],
                        EntityId = string.IsNullOrWhiteSpace(columns[7]) ? null : columns[7],
                        Description = columns.Count > 8 ? columns[8] : string.Empty,
                        DeviceId = columns.Count > 9 ? columns[9] : string.Empty,
                        MetadataJson = columns.Count > 10 ? columns[10] : null,
                        IsActive = true
                    });
                }

                return result
                    .OrderByDescending(x => x.OccurredAtUtc)
                    .Take(maxResults)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("====================================");
                System.Diagnostics.Debug.WriteLine("ERROR LEYENDO AUDITORÍA CSV");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine("====================================");

                return new List<AuditLog>();
            }
        }

        private static async Task AppendAuditLogToCsvAsync(AuditLog auditLog)
        {
            var filePath = GetAuditFilePath();

            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var fileExists = File.Exists(filePath);

            await using var stream = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read);

            await using var writer = new StreamWriter(stream);

            if (!fileExists)
            {
                await writer.WriteLineAsync(
                    "Usuario;UserId;Rol;Fecha;Hora;Accion;Entidad;EntityId;Operacion;Dispositivo;Metadata");
            }

            var line = string.Join(";",
                EscapeCsv(auditLog.UserDisplayName),
                EscapeCsv(auditLog.UserId),
                EscapeCsv(auditLog.RoleCode),
                auditLog.OccurredAtUtc.ToString("dd-MM-yyyy"),
                auditLog.OccurredAtUtc.ToString("HH:mm"),
                EscapeCsv(auditLog.ActionCode),
                EscapeCsv(auditLog.EntityName),
                EscapeCsv(auditLog.EntityId ?? string.Empty),
                EscapeCsv(auditLog.Description),
                EscapeCsv(auditLog.DeviceId),
                EscapeCsv(auditLog.MetadataJson ?? string.Empty));

            await writer.WriteLineAsync(line);
        }

        private static string GetAuditFilePath()
        {
            var directory = Path.Combine(
                FileSystem.AppDataDirectory,
                AuditDirectoryName);

            return Path.Combine(directory, AuditFileName);
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var escaped = value
                .Replace("\"", "\"\"")
                .Replace("\r", " ")
                .Replace("\n", " ");

            if (escaped.Contains(';') || escaped.Contains('"'))
                return $"\"{escaped}\"";

            return escaped;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = string.Empty;
            var insideQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var character = line[i];

                if (character == '"')
                {
                    if (insideQuotes &&
                        i + 1 < line.Length &&
                        line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }

                    continue;
                }

                if (character == ';' && !insideQuotes)
                {
                    result.Add(current);
                    current = string.Empty;
                    continue;
                }

                current += character;
            }

            result.Add(current);

            return result;
        }

        private static string GetUserDisplayNameFromSession(object? session)
        {
            if (session is null)
                return string.Empty;

            var candidatePropertyNames = new[]
            {
                "UserDisplayName",
                "DisplayName",
                "FullName",
                "UserName",
                "Name"
            };

            foreach (var propertyName in candidatePropertyNames)
            {
                var property = session
                    .GetType()
                    .GetProperty(propertyName);

                if (property is null)
                    continue;

                var value = property.GetValue(session)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            var userIdProperty = session
                .GetType()
                .GetProperty("UserId");

            var userId = userIdProperty?.GetValue(session)?.ToString();

            return userId ?? string.Empty;
        }

        public async Task<ExportFileResultDto> ExportCsvAsync(
            DateTime fromDate,
            DateTime toDate)
        {
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
                var sourceFilePath = GetAuditFilePath();

                if (!File.Exists(sourceFilePath))
                {
                    return new ExportFileResultDto
                    {
                        Success = false,
                        Message = "No existe archivo de auditoría local para exportar."
                    };
                }

                var lines = await File.ReadAllLinesAsync(sourceFilePath);

                if (lines.Length <= 1)
                {
                    return new ExportFileResultDto
                    {
                        Success = false,
                        Message = "El archivo de auditoría no tiene registros."
                    };
                }

                var header = lines[0];

                var filteredLines = new List<string>
        {
            header
        };

                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var columns = SplitCsvLine(line);

                    if (columns.Count < 5)
                        continue;

                    var dateText = columns[3];

                    if (!DateTime.TryParseExact(
                            dateText,
                            "dd-MM-yyyy",
                            null,
                            System.Globalization.DateTimeStyles.None,
                            out var auditDate))
                    {
                        continue;
                    }

                    if (auditDate.Date < fromDate.Date ||
                        auditDate.Date > toDate.Date)
                    {
                        continue;
                    }

                    filteredLines.Add(line);
                }

                if (filteredLines.Count == 1)
                {
                    return new ExportFileResultDto
                    {
                        Success = false,
                        Message = "No existen registros de auditoría en el rango seleccionado."
                    };
                }

                var exportDirectory = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "AuditExports");

                Directory.CreateDirectory(exportDirectory);

                var fileName = $"audit_log_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv";
                var filePath = Path.Combine(exportDirectory, fileName);

                await File.WriteAllLinesAsync(
                    filePath,
                    filteredLines,
                    new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                return new ExportFileResultDto
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = fileName,
                    Message = $"Auditoría exportada correctamente: {fileName}"
                };
            }
            catch (Exception ex)
            {
                return new ExportFileResultDto
                {
                    Success = false,
                    Message = $"Error exportando auditoría: {ex.Message}"
                };
            }
        }
    }
}