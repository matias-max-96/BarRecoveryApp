using System.Globalization;
using System.Text;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.ApplicationF.Services.Sync;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using BarRecoveryApp.ApplicationF.Services.Auditing;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class QualityInspectionService : IQualityInspectionService
    {
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly IRepository<BarRecoveryPolicy> _policyRepository;
        private readonly IRepository<QualityInspection> _inspectionRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditLogService _auditLogService;
        private readonly IRepository<BarAttributeDefinition> _attributeDefinitionRepository;
        private readonly IRepository<QualityInspectionAttributeValue> _inspectionAttributeValueRepository;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;
        private readonly ISyncBackgroundRunner _syncBackgroundRunner;

        public QualityInspectionService(
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            IRepository<BarRecoveryPolicy> policyRepository,
            IRepository<QualityInspection> inspectionRepository,
            IRepository<BarAttributeDefinition> attributeDefinitionRepository,
            IRepository<QualityInspectionAttributeValue> inspectionAttributeValueRepository,
            ICurrentUserService currentUserService,
            IAuditLogService auditLogService,
            IRepository<SyncQueueItem> syncQueueRepository,
            ISyncBackgroundRunner syncBackgroundRunner)
        {
            _barRepository = barRepository
                ?? throw new ArgumentNullException(nameof(barRepository));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _policyRepository = policyRepository
                ?? throw new ArgumentNullException(nameof(policyRepository));

            _inspectionRepository = inspectionRepository
                ?? throw new ArgumentNullException(nameof(inspectionRepository));

            _attributeDefinitionRepository = attributeDefinitionRepository
                ?? throw new ArgumentNullException(nameof(attributeDefinitionRepository));

            _inspectionAttributeValueRepository = inspectionAttributeValueRepository
                ?? throw new ArgumentNullException(nameof(inspectionAttributeValueRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));

            _auditLogService = auditLogService
                ?? throw new ArgumentNullException(nameof(auditLogService));

            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));

            _syncBackgroundRunner = syncBackgroundRunner
                ?? throw new ArgumentNullException(nameof(syncBackgroundRunner));
        }

        public async Task<List<BarInspectionTargetDto>> SearchBarsForInspectionAsync(
            string? plantId,
            string? barTypeId,
            string? searchText,
            BarStatus? status,
            bool includeDisposed,
            int? recoveryCountFilter,
            int maxResults)
        {
            var bars = await _barRepository.GetAllAsync();
            var plants = await _plantRepository.GetAllAsync();
            var barTypes = await _barTypeRepository.GetAllAsync();
            var policies = await _policyRepository.GetAllAsync();

            var query = bars.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(plantId))
            {
                query = query.Where(x => x.PlantId == plantId);
            }

            if (!string.IsNullOrWhiteSpace(barTypeId))
            {
                query = query.Where(x => x.BarTypeId == barTypeId);
            }

            var normalizedSearch = NormalizeForSearch(searchText);

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(bar =>
                {
                    var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                    var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                    var barNumber = NormalizeForSearch(bar.BarNumber);
                    var plantName = NormalizeForSearch(plant?.Name);
                    var plantCode = NormalizeForSearch(plant?.Code);
                    var barTypeName = NormalizeForSearch(barType?.Name);
                    var barTypeCode = NormalizeForSearch(barType?.Code);

                    var displayText = NormalizeForSearch(
                        $"{bar.BarNumber} {plant?.Name} {plant?.Code} {barType?.Name} {barType?.Code}");

                    var operationalKey = NormalizeForSearch(
                        $"{plant?.Code}-{barType?.Code}-{bar.BarNumber}");

                    return barNumber.Contains(normalizedSearch) ||
                           plantName.Contains(normalizedSearch) ||
                           plantCode.Contains(normalizedSearch) ||
                           barTypeName.Contains(normalizedSearch) ||
                           barTypeCode.Contains(normalizedSearch) ||
                           displayText.Contains(normalizedSearch) ||
                           operationalKey.Contains(normalizedSearch);
                });
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.CurrentStatus == status.Value);
            }

            if (recoveryCountFilter.HasValue)
            {
                query = query.Where(x => x.RecoveryCount == recoveryCountFilter.Value);
            }

            if (!includeDisposed)
            {
                query = query.Where(x => !x.IsDisposed);
            }

            query = query
                .OrderBy(x => x.PlantId)
                .ThenBy(x => x.BarNumber)
                .Take(maxResults);

            var result = new List<BarInspectionTargetDto>();

            foreach (var bar in query)
            {
                var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                var policy = policies.FirstOrDefault(x =>
                    x.PlantId == bar.PlantId &&
                    x.BarTypeId == bar.BarTypeId &&
                    x.IsActive);

                result.Add(new BarInspectionTargetDto
                {
                    BarId = bar.Id,
                    BarNumber = bar.BarNumber,
                    PlantName = plant?.Name ?? "Planta no encontrada",
                    BarTypeName = barType?.Name ?? "Tipo no encontrado",
                    CurrentRecoveryCount = bar.RecoveryCount,
                    MaxRecoveries = policy?.MaxRecoveries,
                    IsDisposed = bar.IsDisposed,
                    CurrentStatus = bar.CurrentStatus
                });
            }

            return result;
        }


        public async Task<bool> CreateInspectionAsync(
            string barId,
            int recoveryCountAtInspection,
            bool canBeRecovered,
            bool mustBeDisposed,
            bool isApprovedForShipment,
            string? notes,
            List<QualityInspectionAttributeValueInputDto> attributeValues)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("QUALITY_INSPECT"))
                return false;

            var session = _currentUserService.CurrentSession;

            if (session is null)
                return false;

            if (string.IsNullOrWhiteSpace(barId))
                return false;

            if (recoveryCountAtInspection < 0)
                return false;

            if (mustBeDisposed && isApprovedForShipment)
                return false;

            var bar = await _barRepository.GetByIdAsync(barId);

            if (bar is null)
                return false;

            if (!bar.IsActive)
                return false;

            var inspection = new QualityInspection
            {
                Id = Guid.NewGuid().ToString(),
                BarId = bar.Id,
                InspectorUserId = session.UserId,
                InspectionAtUtc = DateTime.Now,
                RecoveryCountAtInspection = recoveryCountAtInspection,
                CanBeRecovered = canBeRecovered,
                MustBeDisposed = mustBeDisposed,
                IsApprovedForShipment = isApprovedForShipment,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                DeviceId = DeviceInfo.Current.Name ?? string.Empty,
                SyncStatus = SyncStatus.Pending,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await _inspectionRepository.InsertAsync(inspection);
            var technicalSummary = await SaveInspectionAttributeValuesAsync(inspection, bar, attributeValues);

            await EnqueueSyncAsync(inspection.Id);

            bar.RecoveryCount = recoveryCountAtInspection;
            bar.UpdatedAtUtc = DateTime.Now;

            if (mustBeDisposed)
            {
                bar.IsDisposed = true;
                bar.CurrentStatus = BarStatus.Disposed;
            }
            else if (isApprovedForShipment)
            {
                bar.IsDisposed = false;
                bar.CurrentStatus = BarStatus.ReadyToShip;
            }
            else if (canBeRecovered)
            {
                bar.IsDisposed = false;
                bar.CurrentStatus = BarStatus.Approved;
            }
            else
            {
                bar.IsDisposed = false;
                bar.CurrentStatus = BarStatus.Rejected;
            }

            await _barRepository.UpdateAsync(bar);

            var plant = await _plantRepository.GetByIdAsync(bar.PlantId);
            var barType = await _barTypeRepository.GetByIdAsync(bar.BarTypeId);

            await _auditLogService.WriteAsync(
                AuditActionCodes.QualityInspectionCreated,
                "QualityInspection",
                inspection.Id,
                $"Se registro inspeccion de calidad para la barra {bar.BarNumber}.",
                    BuildInspectionMetadataJson(
                        bar,
                        plant,
                        barType,
                        inspection,
                        recoveryCountAtInspection,
                        canBeRecovered,
                        mustBeDisposed,
                        isApprovedForShipment,
                        notes,
                        technicalSummary));

            if (mustBeDisposed)
            {
                await _auditLogService.WriteAsync(
                    AuditActionCodes.BarDisposed,
                    "Bar",
                    bar.Id,
                    $"La barra {bar.BarNumber} fue dada de baja en control de calidad.",
                    BuildBarStatusMetadataJson(bar, inspection.Id));
            }
            else if (isApprovedForShipment)
            {
                await _auditLogService.WriteAsync(
                    AuditActionCodes.BarApprovedForShipment,
                    "Bar",
                    bar.Id,
                    $"La barra {bar.BarNumber} fue aprobada para envío.",
                    BuildBarStatusMetadataJson(bar, inspection.Id));
            }
            else if (canBeRecovered)
            {
                await _auditLogService.WriteAsync(
                    AuditActionCodes.BarMarkedRecoverable,
                    "Bar",
                    bar.Id,
                    $"La barra {bar.BarNumber} fue marcada como recuperable nuevamente.",
                    BuildBarStatusMetadataJson(bar, inspection.Id));
            }
            else
            {
                await _auditLogService.WriteAsync(
                    AuditActionCodes.BarRejected,
                    "Bar",
                    bar.Id,
                    $"La barra {bar.BarNumber} fue rechazada en control de calidad.",
                    BuildBarStatusMetadataJson(bar, inspection.Id));
            }

            return true;
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

        private static string BuildInspectionMetadataJson(
    Bar bar,
    Plant? plant,
    BarType? barType,
    QualityInspection inspection,
    int recoveryCountAtInspection,
    bool canBeRecovered,
    bool mustBeDisposed,
    bool isApprovedForShipment,
    string? notes,
    TechnicalAttributeAuditSummary technicalSummary)
        {
            var safeNotes = SafeJsonValue(notes);

            var plantName = plant?.Name ?? "Planta no encontrada";
            var plantCode = plant?.Code ?? string.Empty;

            var barTypeName = barType?.Name ?? "Tipo no encontrado";
            var barTypeCode = barType?.Code ?? string.Empty;

            var outOfRangeAttributes = string.Join(
                ",",
                technicalSummary.OutOfRangeAttributes.Distinct());

            return
                "{" +
                $"\"BarId\":\"{SafeJsonValue(bar.Id)}\"," +
                $"\"BarNumber\":\"{SafeJsonValue(bar.BarNumber)}\"," +
                $"\"PlantName\":\"{SafeJsonValue(plantName)}\"," +
                $"\"PlantCode\":\"{SafeJsonValue(plantCode)}\"," +
                $"\"BarTypeName\":\"{SafeJsonValue(barTypeName)}\"," +
                $"\"BarTypeCode\":\"{SafeJsonValue(barTypeCode)}\"," +
                $"\"InspectionId\":\"{SafeJsonValue(inspection.Id)}\"," +
                $"\"RecoveryCountAtInspection\":{recoveryCountAtInspection}," +
                $"\"CanBeRecovered\":{canBeRecovered.ToString().ToLowerInvariant()}," +
                $"\"MustBeDisposed\":{mustBeDisposed.ToString().ToLowerInvariant()}," +
                $"\"IsApprovedForShipment\":{isApprovedForShipment.ToString().ToLowerInvariant()}," +
                $"\"ResultingStatus\":\"{bar.CurrentStatus}\"," +
                $"\"IsDisposed\":{bar.IsDisposed.ToString().ToLowerInvariant()}," +
                $"\"TechnicalAttributeCount\":{technicalSummary.TechnicalAttributeCount}," +
                $"\"MeasuredAttributeCount\":{technicalSummary.MeasuredAttributeCount}," +
                $"\"OutOfRangeCount\":{technicalSummary.OutOfRangeCount}," +
                $"\"OutOfRangeAttributes\":\"{SafeJsonValue(outOfRangeAttributes)}\"," +
                $"\"HasTechnicalValues\":{technicalSummary.HasTechnicalValues.ToString().ToLowerInvariant()}," +
                $"\"Notes\":\"{safeNotes}\"" +
                "}";
        }
        private static string BuildBarStatusMetadataJson(
                                Bar bar,
                                string inspectionId)
        {
            return
                "{" +
                $"\"BarId\":\"{bar.Id}\"," +
                $"\"BarNumber\":\"{bar.BarNumber}\"," +
                $"\"InspectionId\":\"{inspectionId}\"," +
                $"\"CurrentStatus\":\"{bar.CurrentStatus}\"," +
                $"\"RecoveryCount\":{bar.RecoveryCount}," +
                $"\"IsDisposed\":{bar.IsDisposed.ToString().ToLowerInvariant()}" +
                "}";
        }
        private static string SafeJsonValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .Replace("\\", "\\\\")
                .Replace("\"", "'");
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
        public async Task<List<BarAttributeDefinition>> GetApplicableAttributeDefinitionsAsync(
    string barId)
        {
            if (string.IsNullOrWhiteSpace(barId))
                return new List<BarAttributeDefinition>();

            var bar = await _barRepository.GetByIdAsync(barId);

            if (bar is null)
                return new List<BarAttributeDefinition>();

            var definitions = await _attributeDefinitionRepository.GetAllAsync();

            var applicable = definitions
                .Where(x => x.IsActive)
                .Where(x =>
                    (string.IsNullOrWhiteSpace(x.AppliesToPlantId) ||
                     x.AppliesToPlantId == bar.PlantId) &&
                    (string.IsNullOrWhiteSpace(x.AppliesToBarTypeId) ||
                     x.AppliesToBarTypeId == bar.BarTypeId))
                .Select(x => new
                {
                    Definition = x,
                    Specificity =
                        (!string.IsNullOrWhiteSpace(x.AppliesToPlantId) &&
                         x.AppliesToPlantId == bar.PlantId ? 2 : 0) +
                        (!string.IsNullOrWhiteSpace(x.AppliesToBarTypeId) &&
                         x.AppliesToBarTypeId == bar.BarTypeId ? 1 : 0)
                })
                .GroupBy(x => x.Definition.Code)
                .Select(g => g
                    .OrderByDescending(x => x.Specificity)
                    .ThenBy(x => x.Definition.DisplayOrder)
                    .First()
                    .Definition)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToList();

            return applicable;
        }

        private async Task<TechnicalAttributeAuditSummary> SaveInspectionAttributeValuesAsync(
            QualityInspection inspection,
            Bar bar,
            List<QualityInspectionAttributeValueInputDto> attributeValues)
        {
            var summary = new TechnicalAttributeAuditSummary();

            if (attributeValues is null || attributeValues.Count == 0)
                return summary;

            var definitions = await _attributeDefinitionRepository.GetAllAsync();

            foreach (var input in attributeValues)
            {
                if (string.IsNullOrWhiteSpace(input.AttributeDefinitionId))
                    continue;

                var definition = definitions.FirstOrDefault(
                    x => x.Id == input.AttributeDefinitionId);

                if (definition is null)
                    continue;

                summary.TechnicalAttributeCount++;

                if (input.WasMeasured)
                    summary.MeasuredAttributeCount++;

                var isOutOfRange = CalculateOutOfRange(
                    definition,
                    input);

                if (isOutOfRange == true)
                {
                    summary.OutOfRangeCount++;
                    summary.OutOfRangeAttributes.Add(definition.Code);
                }

                var value = new QualityInspectionAttributeValue
                {
                    Id = Guid.NewGuid().ToString(),

                    QualityInspectionId = inspection.Id,
                    BarId = bar.Id,
                    AttributeDefinitionId = definition.Id,

                    AttributeCode = definition.Code,
                    AttributeName = definition.Name,
                    DataType = definition.DataType,

                    WasMeasured = input.WasMeasured,

                    ValueText = input.ValueText,
                    ValueNumber = input.ValueNumber,
                    ValueDate = input.ValueDate,
                    ValueBool = input.ValueBool,

                    IsOutOfRange = isOutOfRange,

                    MinValueAtInspection = definition.HasRangeValidation
                        ? definition.MinValue
                        : null,

                    MaxValueAtInspection = definition.HasRangeValidation
                        ? definition.MaxValue
                        : null,

                    UnitAtInspection = definition.Unit,
                    ToleranceTextAtInspection = definition.ToleranceText,

                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _inspectionAttributeValueRepository.InsertAsync(value);
            }

            return summary;
        }
        private static bool? CalculateOutOfRange(BarAttributeDefinition definition, QualityInspectionAttributeValueInputDto input)
        {
            if (!input.WasMeasured)
                return null;

            if (!definition.HasRangeValidation)
                return null;

            if (definition.DataType != AttributeDataType.Decimal &&
                definition.DataType != AttributeDataType.Integer)
            {
                return null;
            }

            if (!input.ValueNumber.HasValue)
                return null;

            if (definition.MinValue.HasValue &&
                input.ValueNumber.Value < definition.MinValue.Value)
            {
                return true;
            }

            if (definition.MaxValue.HasValue &&
                input.ValueNumber.Value > definition.MaxValue.Value)
            {
                return true;
            }

            return false;
        }
        private sealed class TechnicalAttributeAuditSummary
        {
            public int TechnicalAttributeCount { get; set; }

            public int MeasuredAttributeCount { get; set; }

            public int OutOfRangeCount { get; set; }

            public List<string> OutOfRangeAttributes { get; set; } = new();

            public bool HasTechnicalValues => TechnicalAttributeCount > 0;
        }

        private async Task EnqueueSyncAsync(string inspectionId)
        {
            // PayloadJson vacío a propósito: QualityInspectionSyncEngine
            // relee el agregado completo (inspección + valores de atributos)
            // directo de los repositorios al momento de subirlo.
            try
            {
                await _syncQueueRepository.InsertAsync(new SyncQueueItem
                {
                    Id = Guid.NewGuid().ToString(),
                    EntityType = "QualityInspection",
                    EntityLocalId = inspectionId,
                    OperationType = SyncOperationType.Create,
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

            }
        }
    }
}