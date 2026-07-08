using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class BarLookupService : IBarLookupService
    {
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;

        public BarLookupService(
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

        public async Task<List<BarLookupResultDto>> SearchBarsAsync(
            string? searchText,
            int maxResults)
        {
            if (!_currentUserService.IsAuthenticated)
                return new List<BarLookupResultDto>();

            var bars = await _barRepository.GetAllAsync();
            var plants = await _plantRepository.GetAllAsync();
            var barTypes = await _barTypeRepository.GetAllAsync();

            var query = bars.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var normalizedSearch = searchText.Trim().ToUpperInvariant();

                query = query.Where(x =>
                    !string.IsNullOrWhiteSpace(x.BarNumber) &&
                    x.BarNumber.ToUpperInvariant().Contains(normalizedSearch));
            }

            query = query
                .OrderBy(x => x.BarNumber)
                .Take(maxResults);

            var result = new List<BarLookupResultDto>();

            foreach (var bar in query)
            {
                var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                result.Add(new BarLookupResultDto
                {
                    BarId = bar.Id,
                    BarNumber = bar.BarNumber,
                    PlantName = plant?.Name ?? "Planta no encontrada",
                    BarTypeName = barType?.Name ?? "Tipo no encontrado",
                    RecoveryCount = bar.RecoveryCount,
                    IsDisposed = bar.IsDisposed,
                    IsActive = bar.IsActive,
                    CurrentStatus = bar.CurrentStatus
                });
            }

            return result;
        }
    }
}