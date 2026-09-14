using BarRecoveryApp.ApplicationF.Services.Auditing;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.ApplicationF.Services.Sync;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using System.Globalization;
using System.Text;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class BarReturnService : IBarReturnService
    {
        // Debe coincidir con el mismo Code usado en QualityInspectionService
        // / QualityInspectionViewModel para identificar el atributo "Peso".
        private const string WeightAttributeCode = "PESO";

        private readonly IRepository<BarReturnReceipt> _receiptRepository;
        private readonly IRepository<BarReturnReceiptBar> _receiptBarRepository;
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditLogService _auditLogService;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;
        private readonly ISyncBackgroundRunner _syncBackgroundRunner;
        private readonly IQualityInspectionService _qualityInspectionService;

        public BarReturnService(
            IRepository<BarReturnReceipt> receiptRepository,
            IRepository<BarReturnReceiptBar> receiptBarRepository,
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            ICurrentUserService currentUserService,
            IAuditLogService auditLogService,
            IRepository<SyncQueueItem> syncQueueRepository,
            ISyncBackgroundRunner syncBackgroundRunner,
            IQualityInspectionService qualityInspectionService)
        {
            _receiptRepository = receiptRepository
                ?? throw new ArgumentNullException(nameof(receiptRepository));

            _receiptBarRepository = receiptBarRepository
                ?? throw new ArgumentNullException(nameof(receiptBarRepository));

            _barRepository = barRepository
                ?? throw new ArgumentNullException(nameof(barRepository));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));

            _auditLogService = auditLogService
                ?? throw new ArgumentNullException(nameof(auditLogService));

            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));

            _syncBackgroundRunner = syncBackgroundRunner
                ?? throw new ArgumentNullException(nameof(syncBackgroundRunner));

            _qualityInspectionService = qualityInspectionService
                ?? throw new ArgumentNullException(nameof(qualityInspectionService));
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

        public async Task<List<ShipmentBarTargetDto>> SearchShippedBarsAsync(
            string? plantId,
            string? barTypeId,
            string? searchText,
            int maxResults)
        {
            var bars = await _barRepository.GetAllAsync();
            var plants = await _plantRepository.GetAllAsync();
            var barTypes = await _barTypeRepository.GetAllAsync();

            var query = bars.AsEnumerable()
                .Where(x =>
                    x.IsActive &&
                    !x.IsDisposed &&
                    x.CurrentStatus == BarStatus.Shipped);

            if (!string.IsNullOrWhiteSpace(plantId))
            {
                query = query.Where(x => x.PlantId == plantId);
            }

            if (!string.IsNullOrWhiteSpace(barTypeId))
            {
                query = query.Where(x => x.BarTypeId == barTypeId);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
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
            }

            query = query
                .OrderBy(x => x.BarNumber)
                .ThenBy(x => x.PlantId)
                .ThenBy(x => x.BarTypeId)
                .Take(maxResults);

            var result = new List<ShipmentBarTargetDto>();

            foreach (var bar in query)
            {
                var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                result.Add(new ShipmentBarTargetDto
                {
                    BarId = bar.Id,
                    BarNumber = bar.BarNumber,
                    PlantName = plant?.Name ?? "Planta no encontrada",
                    BarTypeName = barType?.Name ?? "Tipo no encontrado",
                    RecoveryCount = bar.RecoveryCount,
                    IsSelected = false
                });
            }

            return result;
        }

        public async Task<BarReturnCreateResultDto> CreateReturnReceiptAsync(
            string? returnDocument,
            string? notes,
            List<BarReturnWeightInputDto> barWeights)
        {
            if (!_currentUserService.IsAuthenticated)
                return BarReturnCreateResultDto.Fail("Su sesión no es válida. Vuelva a iniciar sesión.");

            if (!_currentUserService.HasPermission("SHIPMENT_CREATE"))
                return BarReturnCreateResultDto.Fail("No tiene permiso para registrar recepciones.");

            var session = _currentUserService.CurrentSession;

            if (session is null)
                return BarReturnCreateResultDto.Fail("Su sesión no es válida. Vuelva a iniciar sesión.");

            if (barWeights is null || barWeights.Count == 0)
                return BarReturnCreateResultDto.Fail("Debe seleccionar al menos una barra para recepcionar.");

            var distinctWeights = barWeights
                .GroupBy(x => x.BarId)
                .Select(g => g.First())
                .ToList();

            // Primera pasada: valida que exista el atributo "Peso" aplicable
            // para cada barra ANTES de crear nada. El pesaje es obligatorio
            // para el 100% de las barras recepcionadas — si falta configurar
            // el umbral para alguna combinación Planta/TipoBarra, se corta
            // acá en vez de dejar un recibo a medio armar.
            var validBars = new List<(Bar Bar, double WeightKg, string WeightAttributeDefinitionId)>();

            foreach (var input in distinctWeights)
            {
                var bar = await _barRepository.GetByIdAsync(input.BarId);

                if (bar is null || !bar.IsActive || bar.IsDisposed || bar.CurrentStatus != BarStatus.Shipped)
                    continue; // igual que antes: barras ya no válidas se ignoran en silencio

                var applicableDefinitions = await _qualityInspectionService
                    .GetApplicableAttributeDefinitionsAsync(bar.Id);

                var weightDefinition = applicableDefinitions.FirstOrDefault(x =>
                    string.Equals(x.Code, WeightAttributeCode, StringComparison.OrdinalIgnoreCase));

                if (weightDefinition is null)
                {
                    var plant = await _plantRepository.GetByIdAsync(bar.PlantId);
                    var barType = await _barTypeRepository.GetByIdAsync(bar.BarTypeId);

                    return BarReturnCreateResultDto.Fail(
                        $"Falta configurar el atributo \"Peso\" para {plant?.Name ?? "esta planta"} / " +
                        $"{barType?.Name ?? "este tipo de barra"} — no es posible recepcionar la barra " +
                        $"{bar.BarNumber} sin él.");
                }

                validBars.Add((bar, input.WeightKg, weightDefinition.Id));
            }

            if (validBars.Count == 0)
                return BarReturnCreateResultDto.Fail(
                    "Ninguna de las barras seleccionadas está disponible para recepcionar.");

            var receiptId = Guid.NewGuid().ToString();

            var receipt = new BarReturnReceipt
            {
                Id = receiptId,
                ReturnDocument = string.IsNullOrWhiteSpace(returnDocument)
                    ? null
                    : returnDocument.Trim().ToUpperInvariant(),

                ReceivedAtUtc = DateTime.Now,
                ResponsibleUserId = session.UserId,
                Notes = string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim(),

                DeviceId = DeviceInfo.Current.Name ?? string.Empty,
                SyncStatus = SyncStatus.Pending,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await _receiptRepository.InsertAsync(receipt);

            var returnedBars = new List<Bar>();
            var disposedBarNumbers = new List<string>();

            foreach (var (bar, weightKg, weightDefinitionId) in validBars)
            {
                var receiptBar = new BarReturnReceiptBar
                {
                    Id = Guid.NewGuid().ToString(),
                    BarReturnReceiptId = receiptId,
                    BarId = bar.Id,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _receiptBarRepository.InsertAsync(receiptBar);

                // Primero se marca la llegada física de la barra...
                bar.CurrentStatus = BarStatus.Returned;
                bar.UpdatedAtUtc = DateTime.Now;

                await _barRepository.UpdateAsync(bar);

                await EnqueueBarSyncAsync(bar.Id);

                // ...y luego se registra la inspección obligatoria de peso.
                // Se pide "puede recuperarse" como punto de partida — si el
                // peso queda bajo el mínimo configurado para esta
                // Planta/TipoBarra, QualityInspectionService fuerza la baja
                // automáticamente (misma regla que en Inspección de
                // Calidad), anulando lo que se pidió acá.
                await _qualityInspectionService.CreateInspectionAsync(
                    bar.Id,
                    bar.RecoveryCount,
                    canBeRecovered: true,
                    mustBeDisposed: false,
                    isApprovedForShipment: false,
                    notes: string.IsNullOrWhiteSpace(returnDocument)
                        ? "Pesaje obligatorio de recepción de retorno."
                        : $"Pesaje obligatorio de recepción de retorno ({returnDocument.Trim()}).",
                    attributeValues: new List<QualityInspectionAttributeValueInputDto>
                    {
                        new QualityInspectionAttributeValueInputDto
                        {
                            AttributeDefinitionId = weightDefinitionId,
                            WasMeasured = true,
                            ValueNumber = weightKg
                        }
                    });

                // Se relee la barra porque CreateInspectionAsync pudo haber
                // cambiado su estado (ej. a Disposed si el peso quedó bajo
                // el mínimo) — sin esto, el resumen quedaría desactualizado.
                var updatedBar = await _barRepository.GetByIdAsync(bar.Id) ?? bar;

                if (updatedBar.IsDisposed)
                {
                    disposedBarNumbers.Add(updatedBar.BarNumber);
                }

                returnedBars.Add(updatedBar);
            }

            await EnqueueSyncAsync(receiptId);

            var plants = await _plantRepository.GetAllAsync();
            var barTypes = await _barTypeRepository.GetAllAsync();

            await _auditLogService.WriteAsync(
                AuditActionCodes.BarReturnCreated,
                "BarReturnReceipt",
                receiptId,
                $"Se registró recepción de retorno con {returnedBars.Count} barra(s), " +
                $"{disposedBarNumbers.Count} dada(s) de baja automáticamente por peso.",
                BuildReturnReceiptMetadataJson(
                    receipt,
                    returnDocument,
                    notes,
                    returnedBars,
                    plants,
                    barTypes));

            foreach (var bar in returnedBars)
            {
                var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                var operationalLabel = BuildBarOperationalLabel(
                    bar,
                    plant,
                    barType);

                await _auditLogService.WriteAsync(
                    AuditActionCodes.BarReturned,
                    "Bar",
                    bar.Id,
                    $"La barra {operationalLabel} fue marcada como retornada/disponible.",
                    BuildBarReturnedMetadataJson(
                        bar,
                        receiptId,
                        receipt.ReturnDocument,
                        plant,
                        barType));
            }

            return BarReturnCreateResultDto.Ok(disposedBarNumbers);
        }

        private static string BuildReturnReceiptMetadataJson(
    BarReturnReceipt receipt,
    string? returnDocument,
    string? notes,
    List<Bar> returnedBars,
    List<Plant> plants,
    List<BarType> barTypes)
        {
            var safeReturnDocument = string.IsNullOrWhiteSpace(returnDocument)
                ? string.Empty
                : SafeJsonValue(returnDocument);

            var safeNotes = string.IsNullOrWhiteSpace(notes)
                ? string.Empty
                : SafeJsonValue(notes);

            var barsJson = returnedBars
                .Select(bar =>
                {
                    var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                    var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                    var plantName = plant?.Name ?? "Planta no encontrada";
                    var plantCode = plant?.Code ?? string.Empty;

                    var barTypeName = barType?.Name ?? "Tipo no encontrado";
                    var barTypeCode = barType?.Code ?? string.Empty;

                    var operationalKey = BuildBarOperationalKey(
                        bar,
                        plant,
                        barType);

                    return
                        "{" +
                        $"\"BarId\":\"{SafeJsonValue(bar.Id)}\"," +
                        $"\"BarNumber\":\"{SafeJsonValue(bar.BarNumber)}\"," +
                        $"\"PlantId\":\"{SafeJsonValue(bar.PlantId)}\"," +
                        $"\"PlantName\":\"{SafeJsonValue(plantName)}\"," +
                        $"\"PlantCode\":\"{SafeJsonValue(plantCode)}\"," +
                        $"\"BarTypeId\":\"{SafeJsonValue(bar.BarTypeId)}\"," +
                        $"\"BarTypeName\":\"{SafeJsonValue(barTypeName)}\"," +
                        $"\"BarTypeCode\":\"{SafeJsonValue(barTypeCode)}\"," +
                        $"\"OperationalKey\":\"{SafeJsonValue(operationalKey)}\"" +
                        "}";
                });

            var barsJsonText = string.Join(",", barsJson);

            return
                "{" +
                $"\"ReceiptId\":\"{SafeJsonValue(receipt.Id)}\"," +
                $"\"ReturnDocument\":\"{safeReturnDocument}\"," +
                $"\"ReceivedAt\":\"{receipt.ReceivedAtUtc:yyyy-MM-dd HH:mm:ss}\"," +
                $"\"BarCount\":{returnedBars.Count}," +
                $"\"Bars\":[{barsJsonText}]," +
                $"\"Notes\":\"{safeNotes}\"" +
                "}";
        }

        private static string BuildBarReturnedMetadataJson(
    Bar bar,
    string receiptId,
    string? returnDocument,
    Plant? plant,
    BarType? barType)
        {
            var safeReturnDocument = string.IsNullOrWhiteSpace(returnDocument)
                ? string.Empty
                : SafeJsonValue(returnDocument);

            var plantName = plant?.Name ?? "Planta no encontrada";
            var plantCode = plant?.Code ?? string.Empty;

            var barTypeName = barType?.Name ?? "Tipo no encontrado";
            var barTypeCode = barType?.Code ?? string.Empty;

            var operationalKey = BuildBarOperationalKey(
                bar,
                plant,
                barType);

            return
                "{" +
                $"\"BarId\":\"{SafeJsonValue(bar.Id)}\"," +
                $"\"BarNumber\":\"{SafeJsonValue(bar.BarNumber)}\"," +
                $"\"PlantId\":\"{SafeJsonValue(bar.PlantId)}\"," +
                $"\"PlantName\":\"{SafeJsonValue(plantName)}\"," +
                $"\"PlantCode\":\"{SafeJsonValue(plantCode)}\"," +
                $"\"BarTypeId\":\"{SafeJsonValue(bar.BarTypeId)}\"," +
                $"\"BarTypeName\":\"{SafeJsonValue(barTypeName)}\"," +
                $"\"BarTypeCode\":\"{SafeJsonValue(barTypeCode)}\"," +
                $"\"OperationalKey\":\"{SafeJsonValue(operationalKey)}\"," +
                $"\"ReceiptId\":\"{SafeJsonValue(receiptId)}\"," +
                $"\"ReturnDocument\":\"{safeReturnDocument}\"," +
                $"\"CurrentStatus\":\"{bar.CurrentStatus}\"," +
                $"\"RecoveryCount\":{bar.RecoveryCount}," +
                $"\"IsDisposed\":{bar.IsDisposed.ToString().ToLowerInvariant()}" +
                "}";
        }
        private static string BuildBarOperationalKey(
    Bar bar,
    Plant? plant,
    BarType? barType)
        {
            var plantCode = string.IsNullOrWhiteSpace(plant?.Code)
                ? plant?.Name ?? "PLANTA"
                : plant.Code;

            var barTypeCode = string.IsNullOrWhiteSpace(barType?.Code)
                ? barType?.Name ?? "TIPO"
                : barType.Code;

            return $"{NormalizeKeyPart(plantCode)}-{NormalizeKeyPart(barTypeCode)}-{NormalizeKeyPart(bar.BarNumber)}";
        }

        private static string BuildBarOperationalLabel(
            Bar bar,
            Plant? plant,
            BarType? barType)
        {
            var plantName = plant?.Name ?? "Planta no encontrada";
            var barTypeName = barType?.Name ?? "Tipo no encontrado";

            return $"{bar.BarNumber} - {plantName} - {barTypeName}";
        }

        private static string NormalizeKeyPart(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("\"", string.Empty);
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

        private async Task EnqueueSyncAsync(string receiptId)
        {
            // PayloadJson vacío a propósito: BarReturnReceiptSyncEngine
            // relee el agregado completo (recibo + barras) directo de los
            // repositorios al momento de subirlo.
            try
            {
                await _syncQueueRepository.InsertAsync(new SyncQueueItem
                {
                    Id = Guid.NewGuid().ToString(),
                    EntityType = "BarReturnReceipt",
                    EntityLocalId = receiptId,
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
                // No dejamos que un error encolando el sync tumbe la
                // creación del recibo, que ya se guardó correctamente.
                // TODO: logging centralizado.
            }
        }

        private async Task EnqueueBarSyncAsync(string barId)
        {
            // Enqueue separado del de BarReturnReceipt: Bar tiene su
            // propio motor con LWW.
            try
            {
                await _syncQueueRepository.InsertAsync(new SyncQueueItem
                {
                    Id = Guid.NewGuid().ToString(),
                    EntityType = "Bar",
                    EntityLocalId = barId,
                    OperationType = SyncOperationType.Update,
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
                // TODO: logging centralizado.
            }
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