using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IBarService
    {
        Task<List<Bar>> GetBarsAsync();

        Task<List<Plant>> GetActivePlantsAsync();

        Task<List<BarType>> GetActiveBarTypesAsync();

        Task<bool> SaveBarAsync(
            string? barId,
            string barNumber,
            string plantId,
            string barTypeId);

        Task<bool> SetBarActiveStateAsync(
            string barId,
            bool isActive);
    }
}