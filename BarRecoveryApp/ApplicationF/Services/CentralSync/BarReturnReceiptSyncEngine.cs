using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class BarReturnReceiptSyncEngine : IBarReturnReceiptSyncEngine
    {
        private const string EntityType = "BarReturnReceipt";
        private const int MaxPushRetries = 5;

        private readonly IBarReturnReceiptSyncApiClient _apiClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly IRepository<BarReturnReceipt> _receiptRepository;
        private readonly IRepository<BarReturnReceiptBar> _receiptBarRepository;
        private readonly IRepository<SyncState> _syncStateRepository;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;

        public BarReturnReceiptSyncEngine(
            IBarReturnReceiptSyncApiClient apiClient,
            ICentralApiCredentialStore credentialStore,
            IRepository<BarReturnReceipt> receiptRepository,
            IRepository<BarReturnReceiptBar> receiptBarRepository,
            IRepository<SyncState> syncStateRepository,
            IRepository<SyncQueueItem> syncQueueRepository)
        {
            _apiClient = apiClient
                ?? throw new ArgumentNullException(nameof(apiClient));
            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));
            _receiptRepository = receiptRepository
                ?? throw new ArgumentNullException(nameof(receiptRepository));
            _receiptBarRepository = receiptBarRepository
                ?? throw new ArgumentNullException(nameof(receiptBarRepository));
            _syncStateRepository = syncStateRepository
                ?? throw new ArgumentNullException(nameof(syncStateRepository));
            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));
        }

        public async Task<BarReturnReceiptSyncRunResult> SyncAsync()
        {
            var result = new BarReturnReceiptSyncRunResult();

            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                result.NotConfigured = true;
                return result;
            }

            result.Pulled = await PullAsync();
            result.Pushed = await PushAsync();

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
                var exists = await _receiptRepository.FirstOrDefaultAsync(x => x.Id == remote.Id);

                if (exists is not null)
                    continue;

                await InsertFullReceiptAsync(remote);
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

        private async Task InsertFullReceiptAsync(BarReturnReceiptSyncDto remote)
        {
            await _receiptRepository.InsertAsync(new BarReturnReceipt
            {
                Id = remote.Id,
                ReturnDocument = remote.ReturnDocument,
                ReceivedAtUtc = remote.ReceivedAtUtc,
                ResponsibleUserId = remote.ResponsibleUserId,
                Notes = remote.Notes,
                DeviceId = "sync-central",
                SyncStatus = SyncStatus.Synced,
                IsActive = remote.IsActive,
                CreatedAtUtc = remote.CreatedAtUtc,
                UpdatedAtUtc = remote.UpdatedAtUtc
            });

            foreach (var barId in remote.BarIds)
            {
                await _receiptBarRepository.InsertAsync(new BarReturnReceiptBar
                {
                    Id = Guid.NewGuid().ToString(),
                    BarReturnReceiptId = remote.Id,
                    BarId = barId,
                    IsActive = true,
                    CreatedAtUtc = remote.CreatedAtUtc,
                    UpdatedAtUtc = remote.UpdatedAtUtc
                });
            }
        }

        private async Task<int> PushAsync()
        {
            var pending = (await _syncQueueRepository.WhereAsync(x =>
                    x.EntityType == EntityType &&
                    (x.SyncStatus == SyncStatus.Pending || x.SyncStatus == SyncStatus.Error) &&
                    x.Retries < MaxPushRetries))
                .OrderBy(x => x.CreatedAtUtc)
                .ToList();

            var pushed = 0;

            foreach (var item in pending)
            {
                var receipt = await _receiptRepository.FirstOrDefaultAsync(x => x.Id == item.EntityLocalId);

                if (receipt is null)
                {
                    item.SyncStatus = SyncStatus.Error;
                    item.ErrorMessage = "El registro local ya no existe.";
                    item.Retries += 1;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    continue;
                }

                var dto = await BuildDtoAsync(receipt);

                var outcome = await _apiClient.PushAsync(dto);

                if (outcome == BarReturnReceiptPushResult.Success)
                {
                    item.SyncStatus = SyncStatus.Synced;
                    item.ErrorMessage = null;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    pushed++;
                }
                else if (outcome == BarReturnReceiptPushResult.NotAuthenticated)
                {
                    break;
                }
                else
                {
                    item.SyncStatus = SyncStatus.Error;
                    item.ErrorMessage = "Error de red al sincronizar con el backend central.";
                    item.Retries += 1;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                }
            }

            return pushed;
        }

        private async Task<BarReturnReceiptSyncDto> BuildDtoAsync(BarReturnReceipt receipt)
        {
            var bars = await _receiptBarRepository.WhereAsync(
                x => x.BarReturnReceiptId == receipt.Id);

            return new BarReturnReceiptSyncDto
            {
                Id = receipt.Id,
                ReturnDocument = receipt.ReturnDocument,
                ReceivedAtUtc = receipt.ReceivedAtUtc,
                ResponsibleUserId = receipt.ResponsibleUserId,
                Notes = receipt.Notes,
                IsActive = receipt.IsActive,
                CreatedAtUtc = receipt.CreatedAtUtc,
                UpdatedAtUtc = receipt.UpdatedAtUtc,
                BarIds = bars.Select(b => b.BarId).ToList()
            };
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