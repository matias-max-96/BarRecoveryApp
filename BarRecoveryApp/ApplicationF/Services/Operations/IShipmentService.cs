using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IShipmentService
    {
        Task<List<Plant>> GetActivePlantsAsync();

        Task<List<BarType>> GetActiveBarTypesAsync();

        Task<List<ShipmentBarTargetDto>> SearchBarsReadyToShipAsync(
            string? plantId,
            string? barTypeId,
            string? searchText,
            int maxResults);

        Task<ShipmentCreateResultDto> CreateShipmentAsync(
            string transferOrder,
            string? customerReference,
            List<string> barIds);
    }
}