using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public interface IBarAttributeDefinitionService
    {
        Task<List<BarAttributeDefinition>> GetDefinitionsAsync();

        Task<List<Plant>> GetActivePlantsAsync();

        Task<List<BarType>> GetActiveBarTypesAsync();

        Task<SaveDefinitionResult> SaveDefinitionAsync(
                    string? definitionId,
                    string code,
                    string name,
                    BarRecoveryApp.Models.Enums.AttributeDataType dataType,
                    string? unit,
                    bool isRequired,
                    bool hasRangeValidation,
                    double? minValue,
                    double? maxValue,
                    string? toleranceText,
                    string? appliesToPlantId,
                    string? appliesToBarTypeId,
                    int displayOrder);

        Task<bool> SetDefinitionActiveStateAsync(
            string definitionId,
            bool isActive);
    }
}