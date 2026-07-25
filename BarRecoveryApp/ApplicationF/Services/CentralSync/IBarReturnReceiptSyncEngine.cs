namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class BarReturnReceiptSyncRunResult
    {
        public int Pulled { get; set; }

        public int Pushed { get; set; }

        public bool NotConfigured { get; set; }
    }

    public interface IBarReturnReceiptSyncEngine
    {
        Task<BarReturnReceiptSyncRunResult> SyncAsync();
    }
}