namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class QualityInspectionSyncRunResult
    {
        public int Pulled { get; set; }

        public int Pushed { get; set; }

        public bool NotConfigured { get; set; }
    }

    public interface IQualityInspectionSyncEngine
    {
        Task<QualityInspectionSyncRunResult> SyncAsync();
    }
}