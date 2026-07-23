using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class PlantSyncEngine : IPlantSyncEngine
    {
        private const string EntityType = "Plant";
        private const int MaxPushRetries = 5;

        private readonly IPlantSyncApiClient _apiClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<SyncState> _syncStateRepository;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;

        public PlantSyncEngine(
            IPlantSyncApiClient apiClient,
            ICentralApiCredentialStore credentialStore,
            IRepository<Plant> plantRepository,
            IRepository<SyncState> syncStateRepository,
            IRepository<SyncQueueItem> syncQueueRepository)
        {
            _apiClient = apiClient
                ?? throw new ArgumentNullException(nameof(apiClient));

            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _syncStateRepository = syncStateRepository
                ?? throw new ArgumentNullException(nameof(syncStateRepository));

            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));
        }

        public async Task<PlantSyncRunResult> SyncAsync()
        {
            var result = new PlantSyncRunResult();

            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                result.NotConfigured = true;
                return result;
            }

            result.Pulled = await PullAsync();
            (result.Pushed, result.PushConflicts) = await PushAsync();

            return result;
        }

        private async Task<int> PullAsync()
        {
            var syncState = await _syncStateRepository.FirstOrDefaultAsync(
                x => x.EntityType == EntityType);

            var since = syncState?.LastPulledAtUtc;

            var changed = await _apiClient.GetChangedSinceAsync(since);

            if (changed is null)
                return 0; // sin conexión / sin credenciales — no es un error fatal

            var applied = 0;

            foreach (var remote in changed)
            {
                var local = await _plantRepository.FirstOrDefaultAsync(x => x.Id == remote.Id);

                if (local is null)
                {
                    await _plantRepository.InsertAsync(new Plant
                    {
                        Id = remote.Id,
                        Code = remote.Code,
                        Name = remote.Name,
                        Description = remote.Description,
                        IsActive = remote.IsActive,
                        CreatedAtUtc = remote.CreatedAtUtc,
                        UpdatedAtUtc = remote.UpdatedAtUtc
                    });

                    applied++;
                    continue;
                }

                // Mismo criterio de Last-Write-Wins que aplica el servidor:
                // si esta tablet ya tiene algo más nuevo (ej. lo editó
                // offline y aún no lo pushea), no lo pisa con lo que baja.
                if (remote.UpdatedAtUtc <= local.UpdatedAtUtc)
                    continue;

                local.Code = remote.Code;
                local.Name = remote.Name;
                local.Description = remote.Description;
                local.IsActive = remote.IsActive;
                local.UpdatedAtUtc = remote.UpdatedAtUtc;

                await _plantRepository.UpdateAsync(local);
                applied++;
            }

            var newCursor = changed.Count > 0
                ? changed.Max(x => x.UpdatedAtUtc)
                : since;

            if (newCursor.HasValue)
            {
                await TouchSyncStateAsync(newCursor.Value);
            }

            return applied;
        }

        private async Task<(int pushed, int conflicts)> PushAsync()
        {
            var pending = (await _syncQueueRepository.WhereAsync(x =>
                    x.EntityType == EntityType &&
                    (x.SyncStatus == SyncStatus.Pending || x.SyncStatus == SyncStatus.Error) &&
                    x.Retries < MaxPushRetries))
                .OrderBy(x => x.CreatedAtUtc)
                .ToList();

            var pushed = 0;
            var conflicts = 0;

            foreach (var item in pending)
            {
                var plant = await _plantRepository.FirstOrDefaultAsync(x => x.Id == item.EntityLocalId);

                if (plant is null)
                {
                    // La barra/planta local ya no existe — no tiene sentido
                    // seguir reintentando algo que no se puede mandar.
                    item.SyncStatus = SyncStatus.Error;
                    item.ErrorMessage = "El registro local ya no existe.";
                    item.Retries += 1;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    continue;
                }

                var dto = new PlantSyncDto
                {
                    Id = plant.Id,
                    Code = plant.Code,
                    Name = plant.Name,
                    Description = plant.Description,
                    IsActive = plant.IsActive,
                    CreatedAtUtc = plant.CreatedAtUtc,
                    UpdatedAtUtc = plant.UpdatedAtUtc
                };

                var outcome = await _apiClient.PushAsync(dto);

                switch (outcome.Result)
                {
                    case PlantPushResult.Success:
                        item.SyncStatus = SyncStatus.Synced;
                        item.ErrorMessage = null;
                        item.LastAttemptUtc = DateTime.Now;
                        await _syncQueueRepository.UpdateAsync(item);
                        pushed++;
                        break;

                    case PlantPushResult.Conflict:
                        // El servidor tiene una versión más nueva (la subió
                        // otra tablet). Se adopta localmente y se marca como
                        // sincronizado — no tiene sentido reintentar subir
                        // algo que el servidor ya decidió que pierde.
                        if (outcome.ServerVersion is not null)
                        {
                            plant.Code = outcome.ServerVersion.Code;
                            plant.Name = outcome.ServerVersion.Name;
                            plant.Description = outcome.ServerVersion.Description;
                            plant.IsActive = outcome.ServerVersion.IsActive;
                            plant.UpdatedAtUtc = outcome.ServerVersion.UpdatedAtUtc;
                            await _plantRepository.UpdateAsync(plant);
                        }

                        item.SyncStatus = SyncStatus.Synced;
                        item.ErrorMessage = "Se sobrescribió con una versión más reciente de otra tablet.";
                        item.LastAttemptUtc = DateTime.Now;
                        await _syncQueueRepository.UpdateAsync(item);
                        conflicts++;
                        break;

                    case PlantPushResult.NotAuthenticated:
                        // Sin credenciales válidas: reintentar todo el resto
                        // de la cola va a fallar igual, cortamos aquí.
                        return (pushed, conflicts);

                    case PlantPushResult.NetworkError:
                        item.SyncStatus = SyncStatus.Error;
                        item.ErrorMessage = "Error de red al sincronizar con el backend central.";
                        item.Retries += 1;
                        item.LastAttemptUtc = DateTime.Now;
                        await _syncQueueRepository.UpdateAsync(item);
                        break;
                }
            }

            return (pushed, conflicts);
        }

        private async Task TouchSyncStateAsync(DateTime lastPulledAtUtc)
        {
            var state = await _syncStateRepository.FirstOrDefaultAsync(
                x => x.EntityType == EntityType);

            if (state is null)
            {
                await _syncStateRepository.InsertAsync(new SyncState
                {
                    Id = Guid.NewGuid().ToString(),
                    EntityType = EntityType,
                    LastPulledAtUtc = lastPulledAtUtc,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                });
                return;
            }

            state.LastPulledAtUtc = lastPulledAtUtc;
            await _syncStateRepository.UpdateAsync(state);
        }
    }
}