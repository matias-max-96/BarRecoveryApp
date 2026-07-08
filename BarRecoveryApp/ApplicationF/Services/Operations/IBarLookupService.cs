using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IBarLookupService
    {
        Task<List<BarLookupResultDto>> SearchBarsAsync(
            string? searchText,
            int maxResults);
    }
}