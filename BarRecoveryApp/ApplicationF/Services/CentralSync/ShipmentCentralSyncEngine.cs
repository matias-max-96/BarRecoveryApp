using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class ShipmentCentralSyncEngine : IShipmentCentralSyncEngine
    {
        // OJO: distinto de "Shipment" (usado por SyncEngineService para el
        // reporte a Pomerium) — si usáramos el mismo nombre, ese motor
        // agarraría también estas filas y las quemaría con reintentos
        // fallidos, igual que pasó con Plant al principio.
        private const string EntityType = "ShipmentCentral";
        private const int MaxPushRetries = 5;

        private readonly IShipmentCentralSyncApiClient _apiClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly IRepository<Shipment> _shipmentRepository;
        private readonly IRepository<ShipmentBar> _shipmentBarRepository;
        private readonly IRepository<SyncState> _syncStateRepository;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;

        public ShipmentCentralSyncEngine(
            IShipmentCentralSyncApiClient apiClient,
            ICentralApiCredentialStore credentialStore,
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentBar> shipmentBarRepository,
            IRepository<SyncState> syncStateRepository,
            IRepository<SyncQueueItem> syncQueueRepository)
        {
            _apiClient = apiClient
                ?? throw new ArgumentNullException(nameof(apiClient));
            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));
            _shipmentRepository = shipmentRepository
                ?? throw new ArgumentNullException(nameof(shipmentRepository));
            _shipmentBarRepository = shipmentBarRepository
                ?? throw new ArgumentNullException(nameof(shipmentBarRepository));
            _syncStateRepository = syncStateRepository
                ?? throw new ArgumentNullException(nameof(syncStateRepository));
            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));
        }

        public async Task<ShipmentCentralSyncRunResult> SyncAsync()
        {
            var result = new ShipmentCentralSyncRunResult();

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
                var exists = await _shipmentRepository.FirstOrDefaultAsync(x => x.Id == remote.Id);

                if (exists is not null)
                    continue;

                await InsertFullShipmentAsync(remote);
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

        private async Task InsertFullShipmentAsync(ShipmentSyncDto remote)
        {
            await _shipmentRepository.InsertAsync(new Shipment
            {
                Id = remote.Id,
                TransferOrder = remote.TransferOrder,
                CustomerReference = remote.CustomerReference,
                DispatchGuideNumber = remote.DispatchGuideNumber,
                ShippedAtUtc = remote.ShippedAtUtc,
                ResponsibleUserId = remote.ResponsibleUserId,
                DeviceId = "sync-central",
                SyncStatus = SyncStatus.Synced,
                IsActive = remote.IsActive,
                CreatedAtUtc = remote.CreatedAtUtc,
                UpdatedAtUtc = remote.UpdatedAtUtc
            });

            foreach (var barId in remote.BarIds)
            {
                await _shipmentBarRepository.InsertAsync(new ShipmentBar
                {
                    Id = Guid.NewGuid().ToString(),
                    ShipmentId = remote.Id,
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
                var shipment = await _shipmentRepository.FirstOrDefaultAsync(x => x.Id == item.EntityLocalId);

                if (shipment is null)
                {
                    item.SyncStatus = SyncStatus.Error;
                    item.ErrorMessage = "El registro local ya no existe.";
                    item.Retries += 1;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    continue;
                }

                var dto = await BuildDtoAsync(shipment);

                var outcome = await _apiClient.PushAsync(dto);

                if (outcome == ShipmentCentralPushResult.Success)
                {
                    item.SyncStatus = SyncStatus.Synced;
                    item.ErrorMessage = null;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    pushed++;
                }
                else if (outcome == ShipmentCentralPushResult.NotAuthenticated)
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

        private async Task<ShipmentSyncDto> BuildDtoAsync(Shipment shipment)
        {
            var bars = await _shipmentBarRepository.WhereAsync(
                x => x.ShipmentId == shipment.Id);

            return new ShipmentSyncDto
            {
                Id = shipment.Id,
                TransferOrder = shipment.TransferOrder,
                CustomerReference = shipment.CustomerReference,
                DispatchGuideNumber = shipment.DispatchGuideNumber,
                ShippedAtUtc = shipment.ShippedAtUtc,
                ResponsibleUserId = shipment.ResponsibleUserId,
                IsActive = shipment.IsActive,
                CreatedAtUtc = shipment.CreatedAtUtc,
                UpdatedAtUtc = shipment.UpdatedAtUtc,
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