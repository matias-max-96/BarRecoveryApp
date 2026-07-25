using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class RecoveryWorkReportSyncEngine : IRecoveryWorkReportSyncEngine
    {
        private const string EntityType = "RecoveryWorkReport";
        private const int MaxPushRetries = 5;

        private readonly IRecoveryWorkReportSyncApiClient _apiClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly IRepository<RecoveryWorkReport> _reportRepository;
        private readonly IRepository<RecoveryWorkReportCategory> _categoryRepository;
        private readonly IRepository<RecoveryWorkActivity> _activityRepository;
        private readonly IRepository<RecoveryWorkSupply> _supplyRepository;
        private readonly IRepository<SyncState> _syncStateRepository;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;

        public RecoveryWorkReportSyncEngine(
            IRecoveryWorkReportSyncApiClient apiClient,
            ICentralApiCredentialStore credentialStore,
            IRepository<RecoveryWorkReport> reportRepository,
            IRepository<RecoveryWorkReportCategory> categoryRepository,
            IRepository<RecoveryWorkActivity> activityRepository,
            IRepository<RecoveryWorkSupply> supplyRepository,
            IRepository<SyncState> syncStateRepository,
            IRepository<SyncQueueItem> syncQueueRepository)
        {
            _apiClient = apiClient
                ?? throw new ArgumentNullException(nameof(apiClient));
            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));
            _reportRepository = reportRepository
                ?? throw new ArgumentNullException(nameof(reportRepository));
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
            _activityRepository = activityRepository
                ?? throw new ArgumentNullException(nameof(activityRepository));
            _supplyRepository = supplyRepository
                ?? throw new ArgumentNullException(nameof(supplyRepository));
            _syncStateRepository = syncStateRepository
                ?? throw new ArgumentNullException(nameof(syncStateRepository));
            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));
        }

        public async Task<RecoveryWorkReportSyncRunResult> SyncAsync()
        {
            var result = new RecoveryWorkReportSyncRunResult();

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
                // Un reporte nunca se edita después de creado — si ya existe
                // localmente (ej. lo creaste tú mismo y ya lo subiste), no
                // hay nada que actualizar, solo insertar lo que falte.
                var exists = await _reportRepository.FirstOrDefaultAsync(x => x.Id == remote.Id);

                if (exists is not null)
                    continue;

                await InsertFullReportAsync(remote);
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

        private async Task InsertFullReportAsync(RecoveryWorkReportSyncDto remote)
        {
            await _reportRepository.InsertAsync(new RecoveryWorkReport
            {
                Id = remote.Id,
                UserId = remote.UserId,
                WorkDate = remote.WorkDate,
                ShiftName = remote.ShiftName,
                Notes = remote.Notes,
                DeviceId = "sync-central", // vino de otra tablet, no de este dispositivo
                SyncStatus = SyncStatus.Synced,
                IsActive = remote.IsActive,
                CreatedAtUtc = remote.CreatedAtUtc,
                UpdatedAtUtc = remote.UpdatedAtUtc
            });

            foreach (var category in remote.Categories)
            {
                await _categoryRepository.InsertAsync(new RecoveryWorkReportCategory
                {
                    Id = category.Id,
                    RecoveryWorkReportId = remote.Id,
                    WorkType = (ProductionWorkType)category.WorkType,
                    PlantId = category.PlantId,
                    BarTypeId = category.BarTypeId,
                    BarsWorkedCount = category.BarsWorkedCount,
                    ExportLabel = category.ExportLabel,
                    IsActive = true,
                    CreatedAtUtc = remote.CreatedAtUtc,
                    UpdatedAtUtc = remote.UpdatedAtUtc
                });

                foreach (var activity in category.Activities)
                {
                    await _activityRepository.InsertAsync(new RecoveryWorkActivity
                    {
                        Id = activity.Id,
                        RecoveryWorkReportCategoryId = category.Id,
                        ActivityId = activity.ActivityId,
                        HoursWorked = activity.HoursWorked,
                        IsActive = true,
                        CreatedAtUtc = remote.CreatedAtUtc,
                        UpdatedAtUtc = remote.UpdatedAtUtc
                    });
                }

                foreach (var supply in category.Supplies)
                {
                    await _supplyRepository.InsertAsync(new RecoveryWorkSupply
                    {
                        Id = supply.Id,
                        RecoveryWorkReportCategoryId = category.Id,
                        SupplyId = supply.SupplyId,
                        Quantity = supply.Quantity,
                        IsActive = true,
                        CreatedAtUtc = remote.CreatedAtUtc,
                        UpdatedAtUtc = remote.UpdatedAtUtc
                    });
                }
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
                var report = await _reportRepository.FirstOrDefaultAsync(x => x.Id == item.EntityLocalId);

                if (report is null)
                {
                    item.SyncStatus = SyncStatus.Error;
                    item.ErrorMessage = "El registro local ya no existe.";
                    item.Retries += 1;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    continue;
                }

                var dto = await BuildDtoAsync(report);

                var outcome = await _apiClient.PushAsync(dto);

                if (outcome == RecoveryWorkReportPushResult.Success)
                {
                    item.SyncStatus = SyncStatus.Synced;
                    item.ErrorMessage = null;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    pushed++;
                }
                else if (outcome == RecoveryWorkReportPushResult.NotAuthenticated)
                {
                    // Sin credenciales válidas: el resto de la cola va a
                    // fallar igual, cortamos aquí.
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

        private async Task<RecoveryWorkReportSyncDto> BuildDtoAsync(RecoveryWorkReport report)
        {
            var categories = await _categoryRepository.WhereAsync(
                x => x.RecoveryWorkReportId == report.Id);

            var dto = new RecoveryWorkReportSyncDto
            {
                Id = report.Id,
                UserId = report.UserId,
                WorkDate = report.WorkDate,
                ShiftName = report.ShiftName,
                Notes = report.Notes,
                IsActive = report.IsActive,
                CreatedAtUtc = report.CreatedAtUtc,
                UpdatedAtUtc = report.UpdatedAtUtc
            };

            foreach (var category in categories)
            {
                var activities = await _activityRepository.WhereAsync(
                    x => x.RecoveryWorkReportCategoryId == category.Id);

                var supplies = await _supplyRepository.WhereAsync(
                    x => x.RecoveryWorkReportCategoryId == category.Id);

                dto.Categories.Add(new RecoveryWorkReportCategorySyncDto
                {
                    Id = category.Id,
                    WorkType = (int)category.WorkType,
                    PlantId = category.PlantId,
                    BarTypeId = category.BarTypeId,
                    BarsWorkedCount = category.BarsWorkedCount,
                    ExportLabel = category.ExportLabel,
                    Activities = activities.Select(a => new RecoveryWorkActivitySyncDto
                    {
                        Id = a.Id,
                        ActivityId = a.ActivityId,
                        HoursWorked = a.HoursWorked
                    }).ToList(),
                    Supplies = supplies.Select(s => new RecoveryWorkSupplySyncDto
                    {
                        Id = s.Id,
                        SupplyId = s.SupplyId,
                        Quantity = s.Quantity
                    }).ToList()
                });
            }

            return dto;
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