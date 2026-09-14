using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IBarReturnService
    {
        Task<List<Plant>> GetActivePlantsAsync();

        Task<List<BarType>> GetActiveBarTypesAsync();

        Task<List<ShipmentBarTargetDto>> SearchShippedBarsAsync(
            string? plantId,
            string? barTypeId,
            string? searchText,
            int maxResults);

        // El pesaje es obligatorio para el 100% de las barras recepcionadas
        // — por eso ahora recibe el peso de cada barra en vez de solo sus
        // Ids. Si el peso queda bajo el mínimo configurado para esa
        // Planta/TipoBarra, la barra se da de baja automáticamente (mismo
        // mecanismo que en Inspección de Calidad).
        Task<BarReturnCreateResultDto> CreateReturnReceiptAsync(
            string? returnDocument,
            string? notes,
            List<BarReturnWeightInputDto> barWeights);
    }
}