using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IRecoveryWorkReportService
    {
        Task<List<ActivityModel>> GetActiveActivitiesAsync();

        Task<List<Supply>> GetActiveSuppliesAsync();

        Task<bool> CreateReportAsync(
            DateTime workDate,
            string? shiftName,
            int barsWorkedCount,
            string? notes,
            List<RecoveryWorkActivityInput> activities,
            List<RecoveryWorkSupplyInput> supplies);

        Task<List<RecoveryWorkReport>> GetMyReportsAsync();

        Task<List<RecoveryWorkReportItemDto>> GetMyReportItemsAsync();

    }
}