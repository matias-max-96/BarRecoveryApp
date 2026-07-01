using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public interface IPlantService
    {
        Task<List<Plant>> GetPlantsAsync();

        Task<bool> SavePlantAsync(
            string? plantId,
            string code,
            string name,
            string? description);

        Task<bool> SetPlantActiveStateAsync(
            string plantId,
            bool isActive);
    }
}
