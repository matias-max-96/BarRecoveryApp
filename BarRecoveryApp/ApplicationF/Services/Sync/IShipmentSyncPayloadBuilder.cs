using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public interface IShipmentSyncPayloadBuilder
    {
        Task<string> BuildAsync(Shipment shipment, List<Bar> shippedBars);
    }
}