namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class ShipmentCentralSyncRunResult
    {
        public int Pulled { get; set; }

        public int Pushed { get; set; }

        public bool NotConfigured { get; set; }
    }

    public interface IShipmentCentralSyncEngine
    {
        Task<ShipmentCentralSyncRunResult> SyncAsync();
    }
}