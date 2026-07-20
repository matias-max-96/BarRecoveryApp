using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public class SyncEngineService : ISyncEngineService
    {
        private const int MaxRetries = 5;

        private readonly IRepository<SyncQueueItem> _queueRepository;
        private readonly IRepository<SyncState> _syncStateRepository;
        private readonly ISyncApiClient _apiClient;
        private readonly ISyncCredentialStore _credentialStore;

        public SyncEngineService(
            IRepository<SyncQueueItem> queueRepository,
            IRepository<SyncState> syncStateRepository,
            ISyncApiClient apiClient,
            ISyncCredentialStore credentialStore)
        {
            _queueRepository = queueRepository
                ?? throw new ArgumentNullException(nameof(queueRepository));

            _syncStateRepository = syncStateRepository
                ?? throw new ArgumentNullException(nameof(syncStateRepository));

            _apiClient = apiClient
                ?? throw new ArgumentNullException(nameof(apiClient));

            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));
        }

        public async Task<SyncRunSummary> ProcessPendingAsync(int maxItems = 50)
        {
            var summary = new SyncRunSummary();

            var settings = await _credentialStore.GetAsync();

            if (settings is null ||
                string.IsNullOrWhiteSpace(settings.PomeriumCookie) ||
                string.IsNullOrWhiteSpace(settings.CustomerId))
            {
                // Sin credenciales configuradas, ni siquiera vale la pena tocar
                // la cola: fallaría igual en cada item con el mismo motivo.
                summary.RequiresReAuthentication = true;
                return summary;
            }

            var pendingItems = (await _queueRepository.WhereAsync(x =>
                    (x.SyncStatus == SyncStatus.Pending || x.SyncStatus == SyncStatus.Error) &&
                    x.Retries < MaxRetries))
                .OrderBy(x => x.CreatedAtUtc)
                .Take(maxItems)
                .ToList();

            foreach (var item in pendingItems)
            {
                summary.Processed++;

                if (item.OperationType == SyncOperationType.Delete)
                {
                    // La API documentada hoy no expone un endpoint DELETE.
                    // Se marca como error explícito para no reintentar en vano
                    // ni fallar en silencio; requiere definición con el proveedor.
                    await MarkAsErrorAsync(
                        item,
                        "La API remota no soporta eliminación de registros. Pendiente de definir con el proveedor.");

                    summary.Failed++;
                    continue;
                }

                var result = await _apiClient.PostCustomerDataAsync(
                    settings.CustomerId,
                    item.PayloadJson);

                if (result.Success)
                {
                    item.SyncStatus = SyncStatus.Synced;
                    item.LastAttemptUtc = DateTime.Now;
                    item.ErrorMessage = null;

                    await _queueRepository.UpdateAsync(item);
                    await TouchSyncStateAsync(item.EntityType);

                    summary.Succeeded++;
                }
                else
                {
                    await MarkAsErrorAsync(item, result.ErrorMessage ?? "Error desconocido.");

                    summary.Failed++;

                    if (result.RequiresReAuthentication)
                    {
                        summary.RequiresReAuthentication = true;

                        // Si la sesión venció, seguir intentando con los items
                        // restantes va a fallar exactamente igual — cortamos aquí
                        // en vez de gastar N llamadas HTTP destinadas a fallar.
                        break;
                    }
                }
            }

            return summary;
        }

        private async Task MarkAsErrorAsync(SyncQueueItem item, string errorMessage)
        {
            item.Retries += 1;
            item.LastAttemptUtc = DateTime.Now;
            item.ErrorMessage = errorMessage;
            item.SyncStatus = SyncStatus.Error;

            await _queueRepository.UpdateAsync(item);
        }

        private async Task TouchSyncStateAsync(string entityType)
        {
            var state = await _syncStateRepository.FirstOrDefaultAsync(
                x => x.EntityType == entityType);

            if (state is null)
            {
                state = new SyncState
                {
                    Id = Guid.NewGuid().ToString(),
                    EntityType = entityType,
                    LastPulledAtUtc = DateTime.Now,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _syncStateRepository.InsertAsync(state);
                return;
            }

            state.LastPulledAtUtc = DateTime.Now;

            await _syncStateRepository.UpdateAsync(state);
        }
    }
}