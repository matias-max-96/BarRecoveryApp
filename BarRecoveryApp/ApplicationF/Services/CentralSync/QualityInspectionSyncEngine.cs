using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class QualityInspectionSyncEngine : IQualityInspectionSyncEngine
    {
        private const string EntityType = "QualityInspection";
        private const int MaxPushRetries = 5;

        private readonly IQualityInspectionSyncApiClient _apiClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly IRepository<QualityInspection> _inspectionRepository;
        private readonly IRepository<QualityInspectionAttributeValue> _attributeValueRepository;
        private readonly IRepository<SyncState> _syncStateRepository;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;

        public QualityInspectionSyncEngine(
            IQualityInspectionSyncApiClient apiClient,
            ICentralApiCredentialStore credentialStore,
            IRepository<QualityInspection> inspectionRepository,
            IRepository<QualityInspectionAttributeValue> attributeValueRepository,
            IRepository<SyncState> syncStateRepository,
            IRepository<SyncQueueItem> syncQueueRepository)
        {
            _apiClient = apiClient
                ?? throw new ArgumentNullException(nameof(apiClient));
            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));
            _inspectionRepository = inspectionRepository
                ?? throw new ArgumentNullException(nameof(inspectionRepository));
            _attributeValueRepository = attributeValueRepository
                ?? throw new ArgumentNullException(nameof(attributeValueRepository));
            _syncStateRepository = syncStateRepository
                ?? throw new ArgumentNullException(nameof(syncStateRepository));
            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));
        }

        public async Task<QualityInspectionSyncRunResult> SyncAsync()
        {
            var result = new QualityInspectionSyncRunResult();

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
                var exists = await _inspectionRepository.FirstOrDefaultAsync(x => x.Id == remote.Id);

                if (exists is not null)
                    continue;

                await InsertFullInspectionAsync(remote);
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

        private async Task InsertFullInspectionAsync(QualityInspectionSyncDto remote)
        {
            await _inspectionRepository.InsertAsync(new QualityInspection
            {
                Id = remote.Id,
                BarId = remote.BarId,
                InspectorUserId = remote.InspectorUserId,
                InspectionAtUtc = remote.InspectionAtUtc,
                RecoveryCountAtInspection = remote.RecoveryCountAtInspection,
                CanBeRecovered = remote.CanBeRecovered,
                MustBeDisposed = remote.MustBeDisposed,
                IsApprovedForShipment = remote.IsApprovedForShipment,
                Notes = remote.Notes,
                DeviceId = "sync-central",
                SyncStatus = SyncStatus.Synced,
                IsActive = remote.IsActive,
                CreatedAtUtc = remote.CreatedAtUtc,
                UpdatedAtUtc = remote.UpdatedAtUtc
            });

            foreach (var attribute in remote.AttributeValues)
            {
                await _attributeValueRepository.InsertAsync(new QualityInspectionAttributeValue
                {
                    Id = attribute.Id,
                    QualityInspectionId = remote.Id,
                    BarId = attribute.BarId,
                    AttributeDefinitionId = attribute.AttributeDefinitionId,
                    AttributeCode = attribute.AttributeCode,
                    AttributeName = attribute.AttributeName,
                    DataType = (AttributeDataType)attribute.DataType,
                    WasMeasured = attribute.WasMeasured,
                    ValueText = attribute.ValueText,
                    ValueNumber = attribute.ValueNumber,
                    ValueDate = attribute.ValueDate,
                    ValueBool = attribute.ValueBool,
                    IsOutOfRange = attribute.IsOutOfRange,
                    MinValueAtInspection = attribute.MinValueAtInspection,
                    MaxValueAtInspection = attribute.MaxValueAtInspection,
                    UnitAtInspection = attribute.UnitAtInspection,
                    ToleranceTextAtInspection = attribute.ToleranceTextAtInspection,
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
                var inspection = await _inspectionRepository.FirstOrDefaultAsync(x => x.Id == item.EntityLocalId);

                if (inspection is null)
                {
                    item.SyncStatus = SyncStatus.Error;
                    item.ErrorMessage = "El registro local ya no existe.";
                    item.Retries += 1;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    continue;
                }

                var dto = await BuildDtoAsync(inspection);

                var outcome = await _apiClient.PushAsync(dto);

                if (outcome == QualityInspectionPushResult.Success)
                {
                    item.SyncStatus = SyncStatus.Synced;
                    item.ErrorMessage = null;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    pushed++;
                }
                else if (outcome == QualityInspectionPushResult.NotAuthenticated)
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

        private async Task<QualityInspectionSyncDto> BuildDtoAsync(QualityInspection inspection)
        {
            var attributeValues = await _attributeValueRepository.WhereAsync(
                x => x.QualityInspectionId == inspection.Id);

            return new QualityInspectionSyncDto
            {
                Id = inspection.Id,
                BarId = inspection.BarId,
                InspectorUserId = inspection.InspectorUserId,
                InspectionAtUtc = inspection.InspectionAtUtc,
                RecoveryCountAtInspection = inspection.RecoveryCountAtInspection,
                CanBeRecovered = inspection.CanBeRecovered,
                MustBeDisposed = inspection.MustBeDisposed,
                IsApprovedForShipment = inspection.IsApprovedForShipment,
                Notes = inspection.Notes,
                IsActive = inspection.IsActive,
                CreatedAtUtc = inspection.CreatedAtUtc,
                UpdatedAtUtc = inspection.UpdatedAtUtc,
                AttributeValues = attributeValues.Select(a => new QualityInspectionAttributeValueSyncDto
                {
                    Id = a.Id,
                    BarId = a.BarId,
                    AttributeDefinitionId = a.AttributeDefinitionId,
                    AttributeCode = a.AttributeCode,
                    AttributeName = a.AttributeName,
                    DataType = (int)a.DataType,
                    WasMeasured = a.WasMeasured,
                    ValueText = a.ValueText,
                    ValueNumber = a.ValueNumber,
                    ValueDate = a.ValueDate,
                    ValueBool = a.ValueBool,
                    IsOutOfRange = a.IsOutOfRange,
                    MinValueAtInspection = a.MinValueAtInspection,
                    MaxValueAtInspection = a.MaxValueAtInspection,
                    UnitAtInspection = a.UnitAtInspection,
                    ToleranceTextAtInspection = a.ToleranceTextAtInspection
                }).ToList()
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