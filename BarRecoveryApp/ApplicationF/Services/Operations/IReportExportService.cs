using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IReportExportService
    {
        Task<ExportFileResultDto> ExportBarsCsvAsync();
    }
}