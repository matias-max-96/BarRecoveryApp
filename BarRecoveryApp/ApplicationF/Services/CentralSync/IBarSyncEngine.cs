namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class BarSyncRunResult
    {
        public int Pulled { get; set; }

        public int Pushed { get; set; }

        public int PushConflicts { get; set; }

        public bool NotConfigured { get; set; }
    }

    public interface IBarSyncEngine
    {
        Task<BarSyncRunResult> SyncAsync();
    }
}