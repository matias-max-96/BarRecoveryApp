namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class RecoveryWorkReportSyncRunResult
    {
        public int Pulled { get; set; }

        public int Pushed { get; set; }

        public bool NotConfigured { get; set; }
    }

    public interface IRecoveryWorkReportSyncEngine
    {
        Task<RecoveryWorkReportSyncRunResult> SyncAsync();
    }
}