using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public class PlantService : IPlantService
    {
        private readonly IRepository<Plant> _plantRepository;
        private readonly ICurrentUserService _currentUserService;

        public PlantService(
            IRepository<Plant> plantRepository,
            ICurrentUserService currentUserService)
        {
            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
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

            return true;
        }
    }
}