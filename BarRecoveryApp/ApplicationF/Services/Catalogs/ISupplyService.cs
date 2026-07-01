using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public interface ISupplyService
    {
        Task<List<Supply>> GetSuppliesAsync();

        Task<bool> SaveSupplyAsync(
            string? supplyId,
            string code,
            string name,
            string unit,
            string? description);

        Task<bool> SetSupplyActiveStateAsync(
            string supplyId,
            bool isActive);
    }
}