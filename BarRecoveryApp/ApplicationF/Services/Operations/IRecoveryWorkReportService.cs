using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Operations;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IRecoveryWorkReportService
    {
        Task<List<ActivityModel>> GetActiveActivitiesAsync();

        Task<List<Supply>> GetActiveSuppliesAsync();

        Task<List<Plant>> GetActivePlantsAsync();

        Task<List<BarType>> GetActiveBarTypesAsync();

        Task<bool> CreateReportAsync(
            DateTime workDate,
            string? shiftName,
            string? notes,
            List<RecoveryWorkCategoryInput> categories);

        Task<List<RecoveryWorkReport>> GetMyReportsAsync();

        Task<List<RecoveryWorkReportItemDto>> GetMyReportItemsAsync();
    }
}
