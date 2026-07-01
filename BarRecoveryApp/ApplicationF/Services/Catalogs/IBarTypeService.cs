using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public interface IBarTypeService
    {
        Task<List<BarType>> GetBarTypesAsync();

        Task<bool> SaveBarTypeAsync(
            string? barTypeId,
            string code,
            string name,
            string? description);

        Task<bool> SetBarTypeActiveStateAsync(
            string barTypeId,
            bool isActive);
    }
}