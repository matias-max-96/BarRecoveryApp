using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public interface IBarRecoveryPolicyService
    {
        Task<List<BarRecoveryPolicy>> GetPoliciesAsync();

        Task<List<Plant>> GetActivePlantsAsync();

        Task<List<BarType>> GetActiveBarTypesAsync();

        Task<bool> SavePolicyAsync(
            string? policyId,
            string plantId,
            string barTypeId,
            int maxRecoveries);

        Task<bool> SetPolicyActiveStateAsync(
            string policyId,
            bool isActive);
    }
}