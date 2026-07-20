namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public interface ISyncEngineService
    {
        // Procesa items Pending/Error (bajo el límite de reintentos) en orden
        // de creación, hasta maxItems, y devuelve un resumen del resultado.
        Task<SyncRunSummary> ProcessPendingAsync(int maxItems = 50);
    }
}