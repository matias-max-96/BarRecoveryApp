using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class BarService : IBarService
    {
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;

        public BarService(
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            ICurrentUserService currentUserService)
        {
            _barRepository = barRepository
                ?? throw new ArgumentNullException(nameof(barRepository));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<Bar>> GetBarsAsync()
        {
            var bars = await _barRepository.GetAllAsync();

            return bars
                .OrderBy(x => x.PlantId)
                .ThenBy(x => x.BarTypeId)
                .ThenBy(x => x.BarNumber)
                .ToList();
        }

        public async Task<List<Plant>> GetActivePlantsAsync()
        {
            var plants = await _plantRepository.GetActiveAsync();

            return plants
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<List<BarType>> GetActiveBarTypesAsync()
        {
            var barTypes = await _barTypeRepository.GetActiveAsync();

            return barTypes
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<bool> SaveBarAsync(
            string? barId,
            string barNumber,
            string plantId,
            string barTypeId)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("BAR_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(barNumber))
                return false;

            if (string.IsNullOrWhiteSpace(plantId))
                return false;

            if (string.IsNullOrWhiteSpace(barTypeId))
                return false;

            var normalizedBarNumber = barNumber.Trim().ToUpperInvariant();

            var plant = await _plantRepository.GetByIdAsync(plantId);

            if (plant is null || !plant.IsActive)
                return false;

            var barType = await _barTypeRepository.GetByIdAsync(barTypeId);

            if (barType is null || !barType.IsActive)
                return false;

            var existing = await _barRepository.FirstOrDefaultAsync(
                        x => x.BarNumber == normalizedBarNumber &&
                        x.PlantId == plantId &&
                        x.BarTypeId == barTypeId);

            if (existing is not null &&
                existing.Id != barId)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(barId))
            {
                var bar = new Bar
                {
                    Id = Guid.NewGuid().ToString(),
                    BarNumber = normalizedBarNumber,
                    PlantId = plantId,
                    BarTypeId = barTypeId,
                    CurrentStatus = BarStatus.Created,
                    RecoveryCount = 0,
                    IsDisposed = false,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _barRepository.InsertAsync(bar);

                return true;
            }
            else
            {
                var bar = await _barRepository.GetByIdAsync(barId);

                if (bar is null)
                    return false;

                bar.BarNumber = normalizedBarNumber;
                bar.PlantId = plantId;
                bar.BarTypeId = barTypeId;
                bar.UpdatedAtUtc = DateTime.Now;

                await _barRepository.UpdateAsync(bar);

                return true;
            }
        }

        public async Task<bool> SetBarActiveStateAsync(
            string barId,
            bool isActive)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("BAR_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(barId))
                return false;

            var bar = await _barRepository.GetByIdAsync(barId);

            if (bar is null)
                return false;

            bar.IsActive = isActive;
            bar.UpdatedAtUtc = DateTime.Now;

            await _barRepository.UpdateAsync(bar);

            return true;
        }
    }
}