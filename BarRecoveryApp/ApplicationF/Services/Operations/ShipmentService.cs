using BarRecoveryApp.ApplicationF.Services.Auditing;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using System.Globalization;
using System.Text;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class ShipmentService : IShipmentService
    {
        private readonly IRepository<Shipment> _shipmentRepository;
        private readonly IRepository<ShipmentBar> _shipmentBarRepository;
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditLogService _auditLogService;

        public ShipmentService(
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentBar> shipmentBarRepository,
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            ICurrentUserService currentUserService,
            IAuditLogService auditLogService)
        {
            _shipmentRepository = shipmentRepository
                ?? throw new ArgumentNullException(nameof(shipmentRepository));

            _shipmentBarRepository = shipmentBarRepository
                ?? throw new ArgumentNullException(nameof(shipmentBarRepository));

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

        public async Task<List<ShipmentBarTargetDto>> SearchBarsReadyToShipAsync(
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
                    x.CurrentStatus == BarStatus.ReadyToShip);

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

        public async Task<bool> CreateShipmentAsync(
            string transferOrder,
            string? customerReference,
            List<string> barIds)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("SHIPMENT_CREATE"))
                return false;

            var session = _currentUserService.CurrentSession;

            if (session is null)
                return false;

            if (string.IsNullOrWhiteSpace(transferOrder))
                return false;

            if (barIds is null || barIds.Count == 0)
                return false;

            var normalizedTransferOrder = transferOrder.Trim().ToUpperInvariant();
            var normalizedCustomerReference = customerReference?.Trim();

            var shipmentId = Guid.NewGuid().ToString();

            var shipment = new Shipment
            {
                Id = shipmentId,
                TransferOrder = normalizedTransferOrder,
                CustomerReference = string.IsNullOrWhiteSpace(normalizedCustomerReference)
                    ? null
                    : normalizedCustomerReference,
                ShippedAtUtc = DateTime.Now,
                ResponsibleUserId = session.UserId,
                DeviceId = DeviceInfo.Current.Name ?? string.Empty,
                SyncStatus = SyncStatus.Pending,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await _shipmentRepository.InsertAsync(shipment);

            var shippedBars = new List<Bar>();

            foreach (var barId in barIds.Distinct())
            {
                var bar = await _barRepository.GetByIdAsync(barId);

                if (bar is null)
                    continue;

                if (!bar.IsActive || bar.IsDisposed)
                    continue;

                if (bar.CurrentStatus != BarStatus.ReadyToShip)
                    continue;

                var shipmentBar = new ShipmentBar
                {
                    Id = Guid.NewGuid().ToString(),
                    ShipmentId = shipmentId,
                    BarId = bar.Id,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _shipmentBarRepository.InsertAsync(shipmentBar);

                bar.CurrentStatus = BarStatus.Shipped;
                bar.UpdatedAtUtc = DateTime.Now;

                await _barRepository.UpdateAsync(bar);

                shippedBars.Add(bar);
            }
            var plants = await _plantRepository.GetAllAsync();
            var barTypes = await _barTypeRepository.GetAllAsync();

            await _auditLogService.WriteAsync(
                AuditActionCodes.ShipmentCreated,
                "Shipment",
                shipmentId,
                $"Se creo el envio con orden de traslado {normalizedTransferOrder} y {shippedBars.Count} barra(s).",
                BuildShipmentMetadataJson(
                    shipment,
                    normalizedTransferOrder,
                    normalizedCustomerReference,
                    shippedBars,
                    plants,
                    barTypes));

            foreach (var bar in shippedBars)
            {
                var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                var operationalLabel = BuildBarOperationalLabel(
                    bar,
                    plant,
                    barType);

                await _auditLogService.WriteAsync(
                    AuditActionCodes.BarShipped,
                    "Bar",
                    bar.Id,
                    $"La barra {operationalLabel} fue marcada como enviada en la orden {normalizedTransferOrder}.",
                    BuildBarShippedMetadataJson(
                        bar,
                        shipmentId,
                        normalizedTransferOrder,
                        plant,
                        barType));
            }

            return true;
        }
        private static string BuildShipmentMetadataJson(
    Shipment shipment,
    string transferOrder,
    string? customerReference,
    List<Bar> shippedBars,
    List<Plant> plants,
    List<BarType> barTypes)
        {
            var safeCustomerReference = string.IsNullOrWhiteSpace(customerReference)
                ? string.Empty
                : SafeJsonValue(customerReference);

            var barsJson = shippedBars
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
                $"\"ShipmentId\":\"{SafeJsonValue(shipment.Id)}\"," +
                $"\"TransferOrder\":\"{SafeJsonValue(transferOrder)}\"," +
                $"\"CustomerReference\":\"{safeCustomerReference}\"," +
                $"\"ShippedAt\":\"{shipment.ShippedAtUtc:yyyy-MM-dd HH:mm:ss}\"," +
                $"\"BarCount\":{shippedBars.Count}," +
                $"\"Bars\":[{barsJsonText}]" +
                "}";
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

        private static string BuildBarShippedMetadataJson(
    Bar bar,
    string shipmentId,
    string transferOrder,
    Plant? plant,
    BarType? barType)
        {
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
                $"\"ShipmentId\":\"{SafeJsonValue(shipmentId)}\"," +
                $"\"TransferOrder\":\"{SafeJsonValue(transferOrder)}\"," +
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
    }
}