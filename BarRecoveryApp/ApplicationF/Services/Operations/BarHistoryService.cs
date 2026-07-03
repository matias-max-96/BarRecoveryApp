using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class BarHistoryService : IBarHistoryService
    {
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly IRepository<QualityInspection> _inspectionRepository;
        private readonly IRepository<Shipment> _shipmentRepository;
        private readonly IRepository<ShipmentBar> _shipmentBarRepository;
        private readonly IRepository<BarReturnReceipt> _returnReceiptRepository;
        private readonly IRepository<BarReturnReceiptBar> _returnReceiptBarRepository;
        private readonly IRepository<User> _userRepository;
        private readonly ICurrentUserService _currentUserService;

        public BarHistoryService(
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            IRepository<QualityInspection> inspectionRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentBar> shipmentBarRepository,
            IRepository<BarReturnReceipt> returnReceiptRepository,
            IRepository<BarReturnReceiptBar> returnReceiptBarRepository,
            IRepository<User> userRepository,
            ICurrentUserService currentUserService)
        {
            _barRepository = barRepository ?? throw new ArgumentNullException(nameof(barRepository));
            _plantRepository = plantRepository ?? throw new ArgumentNullException(nameof(plantRepository));
            _barTypeRepository = barTypeRepository ?? throw new ArgumentNullException(nameof(barTypeRepository));
            _inspectionRepository = inspectionRepository ?? throw new ArgumentNullException(nameof(inspectionRepository));
            _shipmentRepository = shipmentRepository ?? throw new ArgumentNullException(nameof(shipmentRepository));
            _shipmentBarRepository = shipmentBarRepository ?? throw new ArgumentNullException(nameof(shipmentBarRepository));
            _returnReceiptRepository = returnReceiptRepository ?? throw new ArgumentNullException(nameof(returnReceiptRepository));
            _returnReceiptBarRepository = returnReceiptBarRepository ?? throw new ArgumentNullException(nameof(returnReceiptBarRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
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

        public async Task<List<BarHistorySearchItemDto>> SearchBarsAsync(
            string? plantId,
            string? barTypeId,
            string? searchText,
            bool includeInactive,
            int maxResults)
        {
            var bars = await _barRepository.GetAllAsync();
            var plants = await _plantRepository.GetAllAsync();
            var barTypes = await _barTypeRepository.GetAllAsync();

            var query = bars.AsEnumerable();

            if (!includeInactive)
            {
                query = query.Where(x => x.IsActive);
            }

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
                var normalizedSearch = searchText.Trim().ToUpperInvariant();

                query = query.Where(x =>
                    !string.IsNullOrWhiteSpace(x.BarNumber) &&
                    x.BarNumber.ToUpperInvariant().Contains(normalizedSearch));
            }

            query = query
                .OrderBy(x => x.BarNumber)
                .Take(maxResults);

            var result = new List<BarHistorySearchItemDto>();

            foreach (var bar in query)
            {
                var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                result.Add(new BarHistorySearchItemDto
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

        public async Task<BarHistoryDetailDto?> GetBarHistoryAsync(string barId)
        {
            if (!_currentUserService.IsAuthenticated)
                return null;

            if (string.IsNullOrWhiteSpace(barId))
                return null;

            var bar = await _barRepository.GetByIdAsync(barId);

            if (bar is null)
                return null;

            var plant = await _plantRepository.GetByIdAsync(bar.PlantId);
            var barType = await _barTypeRepository.GetByIdAsync(bar.BarTypeId);

            var users = await _userRepository.GetAllAsync();

            var detail = new BarHistoryDetailDto
            {
                BarId = bar.Id,
                BarNumber = bar.BarNumber,
                PlantName = plant?.Name ?? "Planta no encontrada",
                BarTypeName = barType?.Name ?? "Tipo no encontrado",
                RecoveryCount = bar.RecoveryCount,
                StatusText = GetStatusText(bar.CurrentStatus, bar.IsDisposed),
                IsDisposed = bar.IsDisposed
            };

            await LoadInspectionHistoryAsync(detail, bar.Id, users);
            await LoadShipmentHistoryAsync(detail, bar.Id, users);
            await LoadReturnHistoryAsync(detail, bar.Id, users);

            return detail;
        }

        private async Task LoadInspectionHistoryAsync(
            BarHistoryDetailDto detail,
            string barId,
            List<User> users)
        {
            var inspections = await _inspectionRepository.WhereAsync(
                x => x.BarId == barId && x.IsActive);

            foreach (var inspection in inspections.OrderByDescending(x => x.InspectionAtUtc))
            {
                var inspector = users.FirstOrDefault(x => x.Id == inspection.InspectorUserId);

                detail.Inspections.Add(new QualityInspectionHistoryDto
                {
                    InspectionAt = inspection.InspectionAtUtc,
                    InspectorName = inspector?.DisplayName ?? "Usuario no encontrado",
                    RecoveryCountAtInspection = inspection.RecoveryCountAtInspection,
                    CanBeRecovered = inspection.CanBeRecovered,
                    MustBeDisposed = inspection.MustBeDisposed,
                    IsApprovedForShipment = inspection.IsApprovedForShipment,
                    Notes = inspection.Notes
                });
            }
        }

        private async Task LoadShipmentHistoryAsync(
            BarHistoryDetailDto detail,
            string barId,
            List<User> users)
        {
            var shipmentBars = await _shipmentBarRepository.WhereAsync(
                x => x.BarId == barId && x.IsActive);

            var shipments = await _shipmentRepository.GetAllAsync();

            foreach (var shipmentBar in shipmentBars)
            {
                var shipment = shipments.FirstOrDefault(x => x.Id == shipmentBar.ShipmentId);

                if (shipment is null)
                    continue;

                var responsible = users.FirstOrDefault(x => x.Id == shipment.ResponsibleUserId);

                detail.Shipments.Add(new ShipmentHistoryDto
                {
                    ShippedAt = shipment.ShippedAtUtc,
                    TransferOrder = shipment.TransferOrder,
                    CustomerReference = shipment.CustomerReference,
                    ResponsibleName = responsible?.DisplayName ?? "Usuario no encontrado"
                });
            }

            detail.Shipments = detail.Shipments
                .OrderByDescending(x => x.ShippedAt)
                .ToList();
        }

        private async Task LoadReturnHistoryAsync(
            BarHistoryDetailDto detail,
            string barId,
            List<User> users)
        {
            var receiptBars = await _returnReceiptBarRepository.WhereAsync(
                x => x.BarId == barId && x.IsActive);

            var receipts = await _returnReceiptRepository.GetAllAsync();

            foreach (var receiptBar in receiptBars)
            {
                var receipt = receipts.FirstOrDefault(x => x.Id == receiptBar.BarReturnReceiptId);

                if (receipt is null)
                    continue;

                var responsible = users.FirstOrDefault(x => x.Id == receipt.ResponsibleUserId);

                detail.Returns.Add(new BarReturnHistoryDto
                {
                    ReceivedAt = receipt.ReceivedAtUtc,
                    ReturnDocument = receipt.ReturnDocument,
                    Notes = receipt.Notes,
                    ResponsibleName = responsible?.DisplayName ?? "Usuario no encontrado"
                });
            }

            detail.Returns = detail.Returns
                .OrderByDescending(x => x.ReceivedAt)
                .ToList();
        }

        private static string GetStatusText(BarStatus status, bool isDisposed)
        {
            if (isDisposed)
                return "Dada de baja";

            return status switch
            {
                BarStatus.Created => "Creada",
                BarStatus.InRecovery => "En recuperación",
                BarStatus.PendingQuality => "Pendiente calidad",
                BarStatus.Approved => "Aprobada",
                BarStatus.Rejected => "Rechazada",
                BarStatus.ReadyToShip => "Lista para envío",
                BarStatus.Shipped => "Enviada",
                BarStatus.Disposed => "Dada de baja",
                BarStatus.Returned => "Retornada / Disponible",
                _ => "Desconocido"
            };
        }
    }
}