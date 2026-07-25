using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public enum ShipmentCentralPushResult
    {
        Success,
        NotAuthenticated,
        NetworkError
    }

    public interface IShipmentCentralSyncApiClient
    {
        Task<List<ShipmentSyncDto>?> GetChangedSinceAsync(DateTime? since);

        Task<ShipmentCentralPushResult> PushAsync(ShipmentSyncDto dto);
    }
}