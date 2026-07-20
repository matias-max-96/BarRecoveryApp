namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public interface ISyncBackgroundRunner
    {
        // Inicia el loop periódico. Llamar una sola vez, al arrancar la app.
        void Start();

        void Stop();

        // Fuerza una corrida inmediata (fire-and-forget), sin esperar al
        // próximo ciclo del timer. Pensado para llamarse justo después de
        // encolar un SyncQueueItem nuevo.
        void TriggerNow();
    }
}