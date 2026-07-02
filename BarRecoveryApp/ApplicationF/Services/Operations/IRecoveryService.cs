using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Operations;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IRecoveryService
    {
        Task<List<Bar>> GetAvailableBarsAsync();

        Task<List<ActivityModel>> GetActiveActivitiesAsync();

        Task<List<Supply>> GetActiveSuppliesAsync();

        Task<bool> RegisterRecoveryAsync(
            string barId,
            string activityId,
            string? notes,
            List<RecoverySupplyInput> supplies);
    }
}