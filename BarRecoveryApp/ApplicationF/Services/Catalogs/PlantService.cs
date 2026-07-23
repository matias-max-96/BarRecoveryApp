using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Sync;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public class PlantService : IPlantService
    {
        private readonly IRepository<Plant> _plantRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;
        private readonly ISyncBackgroundRunner _syncBackgroundRunner;

        public PlantService(
            IRepository<Plant> plantRepository,
            ICurrentUserService currentUserService,
            IRepository<SyncQueueItem> syncQueueRepository,
            ISyncBackgroundRunner syncBackgroundRunner)
        {
            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));

            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));

            _syncBackgroundRunner = syncBackgroundRunner
                ?? throw new ArgumentNullException(nameof(syncBackgroundRunner));
        }

        public async Task<List<Plant>> GetPlantsAsync()
        {
            var plants = await _plantRepository.GetAllAsync();

            return plants
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<bool> SavePlantAsync(
            string? plantId,
            string code,
            string name,
            string? description)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("PLANT_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalizedCode = code.Trim().ToUpperInvariant();
            var normalizedName = name.Trim();
            var normalizedDescription = description?.Trim();

            var existing = await _plantRepository.FirstOrDefaultAsync(
                x => x.Code == normalizedCode);

            if (existing is not null &&
                existing.Id != plantId)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(plantId))
            {
                var plant = new Plant
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = normalizedCode,
                    Name = normalizedName,
                    Description = normalizedDescription,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _plantRepository.InsertAsync(plant);

                await EnqueueSyncAsync(plant.Id, SyncOperationType.Create);

                return true;
            }
            else
            {
                var plant = await _plantRepository.GetByIdAsync(plantId);

                if (plant is null)
                    return false;

                plant.Code = normalizedCode;
                plant.Name = normalizedName;
                plant.Description = normalizedDescription;
                plant.UpdatedAtUtc = DateTime.Now;

                await _plantRepository.UpdateAsync(plant);

                await EnqueueSyncAsync(plant.Id, SyncOperationType.Update);

                return true;
            }
        }

        public async Task<bool> SetPlantActiveStateAsync(
            string plantId,
            bool isActive)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("PLANT_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(plantId))
                return false;

            var plant = await _plantRepository.GetByIdAsync(plantId);

            if (plant is null)
                return false;

            plant.IsActive = isActive;
            plant.UpdatedAtUtc = DateTime.Now;

            await _plantRepository.UpdateAsync(plant);

            await EnqueueSyncAsync(plant.Id, SyncOperationType.Update);

            return true;
        }

        private async Task EnqueueSyncAsync(string plantId, SyncOperationType operationType)
        {
            // No usamos PayloadJson acá (a diferencia del sync con Pomerium):
            // PlantSyncEngine lee el registro actual de Plant directo desde
            // el repositorio al momento de subirlo, así siempre manda el
            // estado más reciente aunque hayan pasado varios cambios entre
            // que se encoló y que efectivamente se sincronizó.
            try
            {
                await _syncQueueRepository.InsertAsync(new SyncQueueItem
                {
                    Id = Guid.NewGuid().ToString(),
                    EntityType = "Plant",
                    EntityLocalId = plantId,
                    OperationType = operationType,
                    PayloadJson = string.Empty,
                    SyncStatus = SyncStatus.Pending,
                    Retries = 0,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                });

                _syncBackgroundRunner.TriggerNow();
            }
            catch (Exception)
            {
                // No dejamos que un error encolando el sync tumbe la
                // operación local, que ya se guardó correctamente.
                // TODO: logging centralizado.
            }
        }
    }
}