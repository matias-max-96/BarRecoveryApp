using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public class BarAttributeDefinitionService : IBarAttributeDefinitionService
    {
        private readonly IRepository<BarAttributeDefinition> _definitionRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;

        public BarAttributeDefinitionService(
            IRepository<BarAttributeDefinition> definitionRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            ICurrentUserService currentUserService)
        {
            _definitionRepository = definitionRepository
                ?? throw new ArgumentNullException(nameof(definitionRepository));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<BarAttributeDefinition>> GetDefinitionsAsync()
        {
            var definitions = await _definitionRepository.GetAllAsync();

            return definitions
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
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

        public async Task<bool> SaveDefinitionAsync(
                                string? definitionId,
                                string code,
                                string name,
                                AttributeDataType dataType,
                                string? unit,
                                bool isRequired,
                                bool hasRangeValidation,
                                double? minValue,
                                double? maxValue,
                                string? toleranceText,
                                string? appliesToPlantId,
                                string? appliesToBarTypeId,
                                int displayOrder)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("ATTRIBUTE_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            var session = _currentUserService.CurrentSession;

            if (session is null)
                return false;

            if (hasRangeValidation)
            {

                if (dataType != AttributeDataType.Decimal &&
                    dataType != AttributeDataType.Integer)
                {
                    return false;
                }

                if (!minValue.HasValue || !maxValue.HasValue)
                    return false;

                if (minValue.Value > maxValue.Value)
                    return false;
            }

            var normalizedCode = code.Trim().ToUpperInvariant();
            var normalizedName = name.Trim();
            var normalizedUnit = unit?.Trim();

            var normalizedPlantId = string.IsNullOrWhiteSpace(appliesToPlantId)
                ? null
                : appliesToPlantId;

            var normalizedBarTypeId = string.IsNullOrWhiteSpace(appliesToBarTypeId)
                ? null
                : appliesToBarTypeId;

            var existing = await _definitionRepository.FirstOrDefaultAsync(
                    x => x.Code == normalizedCode &&
                    x.AppliesToPlantId == normalizedPlantId &&
                    x.AppliesToBarTypeId == normalizedBarTypeId);

            if (existing is not null &&
                existing.Id != definitionId)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(definitionId))
            {
                var definition = new BarAttributeDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = normalizedCode,
                    Name = normalizedName,
                    DataType = dataType,
                    Unit = normalizedUnit,
                    IsRequired = isRequired,

                    HasRangeValidation = hasRangeValidation,
                    MinValue = hasRangeValidation ? minValue : null,
                    MaxValue = hasRangeValidation ? maxValue : null,
                    ToleranceText = string.IsNullOrWhiteSpace(toleranceText)
                                            ? null
                                            : toleranceText.Trim(),

                    AppliesToPlantId = normalizedPlantId,
                    AppliesToBarTypeId = normalizedBarTypeId,
                    DisplayOrder = displayOrder,
                    CreatedByUserId = session.UserId,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _definitionRepository.InsertAsync(definition);

                return true;
            }
            else
            {
                var definition = await _definitionRepository.GetByIdAsync(definitionId);

                if (definition is null)
                    return false;

                definition.Code = normalizedCode;
                definition.Name = normalizedName;
                definition.DataType = dataType;
                definition.Unit = normalizedUnit;
                definition.IsRequired = isRequired;

                definition.HasRangeValidation = hasRangeValidation;
                definition.MinValue = hasRangeValidation ? minValue : null;
                definition.MaxValue = hasRangeValidation ? maxValue : null;
                definition.ToleranceText = string.IsNullOrWhiteSpace(toleranceText)
                    ? null
                    : toleranceText.Trim();

                definition.AppliesToPlantId = normalizedPlantId;
                definition.AppliesToBarTypeId = normalizedBarTypeId;
                definition.DisplayOrder = displayOrder;
                definition.UpdatedAtUtc = DateTime.Now;

                await _definitionRepository.UpdateAsync(definition);

                return true;
            }
        }

        public async Task<bool> SetDefinitionActiveStateAsync(
            string definitionId,
            bool isActive)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("ATTRIBUTE_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(definitionId))
                return false;

            var definition = await _definitionRepository.GetByIdAsync(definitionId);

            if (definition is null)
                return false;

            definition.IsActive = isActive;
            definition.UpdatedAtUtc = DateTime.Now;

            await _definitionRepository.UpdateAsync(definition);

            return true;
        }
    }
}
