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
            int? recoveryCountFilter,
            int maxResults);

        Task<List<BarAttributeDefinition>> GetApplicableAttributeDefinitionsAsync(
            string barId);

        Task<bool> CreateInspectionAsync(
            string barId,
            int recoveryCountAtInspection,
            bool canBeRecovered,
            bool mustBeDisposed,
            bool isApprovedForShipment,
            string? notes,
            List<QualityInspectionAttributeValueInputDto> attributeValues);

        Task<List<Plant>> GetActivePlantsAsync();

        Task<List<BarType>> GetActiveBarTypesAsync();
    }
}