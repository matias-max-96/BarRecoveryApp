using System.Globalization;
using System.Text;
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

            var normalizedSearch = NormalizeForSearch(searchText);

            var result = new List<BarLookupResultDto>();

            foreach (var bar in bars
                         .OrderBy(x => x.BarNumber)
                         .ThenBy(x => x.PlantId)
                         .ThenBy(x => x.BarTypeId))
            {
                var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    var barNumber = NormalizeForSearch(bar.BarNumber);
                    var plantName = NormalizeForSearch(plant?.Name);
                    var plantCode = NormalizeForSearch(plant?.Code);
                    var barTypeName = NormalizeForSearch(barType?.Name);
                    var barTypeCode = NormalizeForSearch(barType?.Code);

                    var displayText = NormalizeForSearch(
                        $"{bar.BarNumber} {plant?.Name} {plant?.Code} {barType?.Name} {barType?.Code}");

                    var operationalKey = NormalizeForSearch(
                        $"{plant?.Code}-{barType?.Code}-{bar.BarNumber}");

                    var matches =
                        barNumber.Contains(normalizedSearch) ||
                        plantName.Contains(normalizedSearch) ||
                        plantCode.Contains(normalizedSearch) ||
                        barTypeName.Contains(normalizedSearch) ||
                        barTypeCode.Contains(normalizedSearch) ||
                        displayText.Contains(normalizedSearch) ||
                        operationalKey.Contains(normalizedSearch);

                    if (!matches)
                        continue;
                }

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

                if (result.Count >= maxResults)
                    break;
            }

            return result;
        }

        private static string NormalizeForSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value
                .Trim()
                .ToUpperInvariant()
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder();

            foreach (var character in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);

                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            return builder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty);
        }
    }
}