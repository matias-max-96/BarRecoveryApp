using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class BarReturnService : IBarReturnService
    {
        private readonly IRepository<BarReturnReceipt> _receiptRepository;
        private readonly IRepository<BarReturnReceiptBar> _receiptBarRepository;
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;

        public BarReturnService(
            IRepository<BarReturnReceipt> receiptRepository,
            IRepository<BarReturnReceiptBar> receiptBarRepository,
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            ICurrentUserService currentUserService)
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
                var normalizedSearch = searchText.Trim().ToUpperInvariant();

                query = query.Where(x =>
                    !string.IsNullOrWhiteSpace(x.BarNumber) &&
                    x.BarNumber.ToUpperInvariant().Contains(normalizedSearch));
            }

            query = query
                .OrderBy(x => x.PlantId)
                .ThenBy(x => x.BarNumber)
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

        public async Task<bool> CreateReturnReceiptAsync(
            string? returnDocument,
            string? notes,
            List<string> barIds)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            /*
             * Por ahora reutilizamos SHIPMENT_CREATE porque es un flujo logístico/calidad.
             * Más adelante podemos crear permiso específico: BAR_RETURN_CREATE.
             */
            if (!_currentUserService.HasPermission("SHIPMENT_CREATE"))
                return false;

            var session = _currentUserService.CurrentSession;

            if (session is null)
                return false;

            if (barIds is null || barIds.Count == 0)
                return false;

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

            foreach (var barId in barIds.Distinct())
            {
                var bar = await _barRepository.GetByIdAsync(barId);

                if (bar is null)
                    continue;

                if (!bar.IsActive)
                    continue;

                if (bar.IsDisposed)
                    continue;

                if (bar.CurrentStatus != BarStatus.Shipped)
                    continue;

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

                bar.CurrentStatus = BarStatus.Returned;
                bar.UpdatedAtUtc = DateTime.Now;

                await _barRepository.UpdateAsync(bar);
            }

            return true;
        }
    }
}