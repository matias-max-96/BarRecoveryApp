using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IBarHistoryService
    {
        Task<List<Plant>> GetActivePlantsAsync();

        Task<List<BarType>> GetActiveBarTypesAsync();

        Task<List<BarHistorySearchItemDto>> SearchBarsAsync(
            string? plantId,
            string? barTypeId,
            string? searchText,
            bool includeInactive,
            int maxResults);

        Task<BarHistoryDetailDto?> GetBarHistoryAsync(string barId);
    }
}