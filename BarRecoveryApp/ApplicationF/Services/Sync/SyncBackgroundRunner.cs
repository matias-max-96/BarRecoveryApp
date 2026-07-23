using BarRecoveryApp.ApplicationF.Services.CentralSync;
using Microsoft.Extensions.DependencyInjection;

namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    // Corre mientras la app está en memoria (foreground o background reciente
    // según el OS). No sobrevive a que el usuario cierre la app del todo —
    // eso requeriría WorkManager (Android) / BGTaskScheduler (iOS), que es
    // una tarea aparte, específica por plataforma.
    public class SyncBackgroundRunner : ISyncBackgroundRunner, IDisposable
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

        private readonly IServiceProvider _serviceProvider;

        private readonly SemaphoreSlim _runGate = new(1, 1);

        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public SyncBackgroundRunner(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider
                ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Start()
        {
            if (_loopTask is not null)
                return; // ya está corriendo, no duplicar el loop

            _cts = new CancellationTokenSource();
            _loopTask = RunLoopAsync(_cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        public void TriggerNow()
        {
            // Fire-and-forget intencional: quien encola un item no debe
            // esperar a que termine de sincronizar para seguir su flujo.
            _ = RunOnceSafeAsync();
        }

        private async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await RunOnceSafeAsync();

                try
                {
                    await Task.Delay(Interval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RunOnceSafeAsync()
        {
            // Evita que el timer y un TriggerNow() manual corran al mismo
            // tiempo y proceven el mismo SyncQueueItem dos veces en paralelo.
            if (!await _runGate.WaitAsync(0))
                return;

            try
            {
                // Cada corrida crea su propio scope: los repositorios y
                // servicios están registrados como Transient, y este runner
                // vive como Singleton durante toda la vida de la app — no
                // queremos retener instancias creadas al arrancar.
                using var scope = _serviceProvider.CreateScope();

                // Dos sistemas de sync independientes, cada uno con su propio
                // try/catch: si uno falla, no debe impedir que el otro corra.
                try
                {
                    var pomeriumEngine = scope.ServiceProvider.GetRequiredService<ISyncEngineService>();
                    await pomeriumEngine.ProcessPendingAsync();
                }
                catch (Exception)
                {
                    // TODO: logging centralizado.
                }

                try
                {
                    var plantSyncEngine = scope.ServiceProvider.GetRequiredService<IPlantSyncEngine>();
                    await plantSyncEngine.SyncAsync();
                }
                catch (Exception)
                {
                    // TODO: logging centralizado.
                }
            }
            catch (Exception)
            {
                // Un error de sync nunca debe tumbar el loop en background.
                // TODO: cuando exista un servicio de logging centralizado,
                // registrar el error acá para poder monitorear fallas de sync.
            }
            finally
            {
                _runGate.Release();
            }
        }

        public void Dispose()
        {
            Stop();
            _runGate.Dispose();
        }
    }
}