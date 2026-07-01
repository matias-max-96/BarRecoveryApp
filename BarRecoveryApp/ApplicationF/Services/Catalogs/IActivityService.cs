using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public interface IActivityService
    {
        Task<List<ActivityModel>> GetActivitiesAsync();

        Task<bool> SaveActivityAsync(
            string? activityId,
            string code,
            string name,
            string? description);

        Task<bool> SetActivityActiveStateAsync(
            string activityId,
            bool isActive);
    }
}