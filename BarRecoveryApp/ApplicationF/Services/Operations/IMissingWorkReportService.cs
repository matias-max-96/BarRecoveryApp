using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IMissingWorkReportService
    {
        Task<List<MissingWorkReportItemDto>> GetMissingWorkReportsAsync(
            DateTime fromDate,
            DateTime toDate);
    }
}