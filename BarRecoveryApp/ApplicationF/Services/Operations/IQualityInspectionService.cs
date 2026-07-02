using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IQualityInspectionService
    {
        Task<List<BarInspectionTargetDto>> SearchBarsForInspectionAsync(
            string? plantId,
            string? barTypeId,
            string? searchText,
            BarStatus? status,
            bool includeDisposed,
            int maxResults);

        Task<bool> CreateInspectionAsync(
            string barId,
            int recoveryCountAtInspection,
            bool canBeRecovered,
            bool mustBeDisposed,
            bool isApprovedForShipment,
            string? notes);

        Task<List<Plant>> GetActivePlantsAsync();

        Task<List<BarType>> GetActiveBarTypesAsync();
    }
}