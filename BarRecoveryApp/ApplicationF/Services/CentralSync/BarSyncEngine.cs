using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class BarSyncEngine : IBarSyncEngine
    {
        private const string EntityType = "Bar";
        private const int MaxPushRetries = 5;

        private readonly IBarSyncApiClient _apiClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<SyncState> _syncStateRepository;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;

        public BarSyncEngine(
            IBarSyncApiClient apiClient,
            ICentralApiCredentialStore credentialStore,
            IRepository<Bar> barRepository,
            IRepository<SyncState> syncStateRepository,
            IRepository<SyncQueueItem> syncQueueRepository)
        {
            _apiClient = apiClient
                ?? throw new ArgumentNullException(nameof(apiClient));

            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));

            _barRepository = barRepository
                ?? throw new ArgumentNullException(nameof(barRepository));

            _syncStateRepository = syncStateRepository
                ?? throw new ArgumentNullException(nameof(syncStateRepository));

            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));
        }

        public async Task<BarSyncRunResult> SyncAsync()
        {
            var result = new BarSyncRunResult();

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
                return 0;

            var applied = 0;

            foreach (var remote in changed)
            {
                var local = await _barRepository.FirstOrDefaultAsync(x => x.Id == remote.Id);

                if (local is null)
                {
                    await _barRepository.InsertAsync(new Bar
                    {
                        Id = remote.Id,
                        BarNumber = remote.BarNumber,
                        PlantId = remote.PlantId,
                        BarTypeId = remote.BarTypeId,
                        CurrentStatus = (BarStatus)remote.CurrentStatus,
                        RecoveryCount = remote.RecoveryCount,
                        IsDisposed = remote.IsDisposed,
                        IsActive = remote.IsActive,
                        CreatedAtUtc = remote.CreatedAtUtc,
                        UpdatedAtUtc = remote.UpdatedAtUtc
                    });

                    applied++;
                    continue;
                }

                // Mismo criterio de LWW que el servidor: si esta tablet ya
                // tiene algo más nuevo (lo editó offline, aún no lo pushea),
                // no lo pisa con lo que baja.
                if (remote.UpdatedAtUtc <= local.UpdatedAtUtc)
                    continue;

                local.BarNumber = remote.BarNumber;
                local.PlantId = remote.PlantId;
                local.BarTypeId = remote.BarTypeId;
                local.CurrentStatus = (BarStatus)remote.CurrentStatus;
                local.RecoveryCount = remote.RecoveryCount;
                local.IsDisposed = remote.IsDisposed;
                local.IsActive = remote.IsActive;
                local.UpdatedAtUtc = remote.UpdatedAtUtc;

                await _barRepository.UpdateAsync(local);
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
                var bar = await _barRepository.FirstOrDefaultAsync(x => x.Id == item.EntityLocalId);

                if (bar is null)
                {
                    item.SyncStatus = SyncStatus.Error;
                    item.ErrorMessage = "El registro local ya no existe.";
                    item.Retries += 1;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    continue;
                }

                var dto = new BarSyncDto
                {
                    Id = bar.Id,
                    BarNumber = bar.BarNumber,
                    PlantId = bar.PlantId,
                    BarTypeId = bar.BarTypeId,
                    CurrentStatus = (int)bar.CurrentStatus,
                    RecoveryCount = bar.RecoveryCount,
                    IsDisposed = bar.IsDisposed,
                    IsActive = bar.IsActive,
                    CreatedAtUtc = bar.CreatedAtUtc,
                    UpdatedAtUtc = bar.UpdatedAtUtc
                };

                var outcome = await _apiClient.PushAsync(dto);

                switch (outcome.Result)
                {
                    case BarPushResult.Success:
                        item.SyncStatus = SyncStatus.Synced;
                        item.ErrorMessage = null;
                        item.LastAttemptUtc = DateTime.Now;
                        await _syncQueueRepository.UpdateAsync(item);
                        pushed++;
                        break;

                    case BarPushResult.Conflict:
                        // El servidor tiene una versión más nueva (otra
                        // tablet la subió primero). Se adopta localmente.
                        if (outcome.ServerVersion is not null)
                        {
                            bar.BarNumber = outcome.ServerVersion.BarNumber;
                            bar.PlantId = outcome.ServerVersion.PlantId;
                            bar.BarTypeId = outcome.ServerVersion.BarTypeId;
                            bar.CurrentStatus = (BarStatus)outcome.ServerVersion.CurrentStatus;
                            bar.RecoveryCount = outcome.ServerVersion.RecoveryCount;
                            bar.IsDisposed = outcome.ServerVersion.IsDisposed;
                            bar.IsActive = outcome.ServerVersion.IsActive;
                            bar.UpdatedAtUtc = outcome.ServerVersion.UpdatedAtUtc;
                            await _barRepository.UpdateAsync(bar);
                        }

                        item.SyncStatus = SyncStatus.Synced;
                        item.ErrorMessage = "Se sobrescribió con una versión más reciente de otra tablet.";
                        item.LastAttemptUtc = DateTime.Now;
                        await _syncQueueRepository.UpdateAsync(item);
                        conflicts++;
                        break;

                    case BarPushResult.NotAuthenticated:
                        return (pushed, conflicts);

                    case BarPushResult.NetworkError:
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