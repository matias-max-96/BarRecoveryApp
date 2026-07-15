using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Auditing
{
    public interface IAuditLogService
    {
        Task WriteAsync(
            string actionCode,
            string entityName,
            string? entityId,
            string description,
            string? metadataJson = null);

        Task<List<AuditLog>> GetRecentAsync(int maxResults);

        Task<ExportFileResultDto> ExportCsvAsync(
            DateTime fromDate,
            DateTime toDate);
    }
}