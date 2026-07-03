using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class DashboardService : IDashboardService
    {
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<RecoveryWorkReport> _recoveryReportRepository;
        private readonly IRepository<Shipment> _shipmentRepository;
        private readonly IRepository<BarReturnReceipt> _returnReceiptRepository;
        private readonly ICurrentUserService _currentUserService;

        public DashboardService(
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<RecoveryWorkReport> recoveryReportRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<BarReturnReceipt> returnReceiptRepository,
            ICurrentUserService currentUserService)
        {
            _barRepository = barRepository
                ?? throw new ArgumentNullException(nameof(barRepository));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _recoveryReportRepository = recoveryReportRepository
                ?? throw new ArgumentNullException(nameof(recoveryReportRepository));

            _shipmentRepository = shipmentRepository
                ?? throw new ArgumentNullException(nameof(shipmentRepository));

            _returnReceiptRepository = returnReceiptRepository
                ?? throw new ArgumentNullException(nameof(returnReceiptRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync()
        {
            if (!_currentUserService.IsAuthenticated)
                return new DashboardSummaryDto();

            var bars = await _barRepository.GetAllAsync();
            var plants = await _plantRepository.GetAllAsync();
            var recoveryReports = await _recoveryReportRepository.GetAllAsync();
            var shipments = await _shipmentRepository.GetAllAsync();
            var returns = await _returnReceiptRepository.GetAllAsync();

            var summary = new DashboardSummaryDto
            {
                TotalBars = bars.Count,
                ActiveBars = bars.Count(x => x.IsActive),
                InactiveBars = bars.Count(x => !x.IsActive),
                DisposedBars = bars.Count(x => x.IsDisposed || x.CurrentStatus == BarStatus.Disposed),
                ReadyToShipBars = bars.Count(x => x.CurrentStatus == BarStatus.ReadyToShip),
                ShippedBars = bars.Count(x => x.CurrentStatus == BarStatus.Shipped),
                ReturnedBars = bars.Count(x => x.CurrentStatus == BarStatus.Returned),
                CreatedBars = bars.Count(x => x.CurrentStatus == BarStatus.Created),
                ApprovedBars = bars.Count(x => x.CurrentStatus == BarStatus.Approved),
                RejectedBars = bars.Count(x => x.CurrentStatus == BarStatus.Rejected),
                TotalRecoveryReports = recoveryReports.Count,
                TotalShipments = shipments.Count,
                TotalReturns = returns.Count
            };

            foreach (var plant in plants.OrderBy(x => x.Name))
            {
                var plantBars = bars
                    .Where(x => x.PlantId == plant.Id)
                    .ToList();

                summary.BarsByPlant.Add(new PlantBarSummaryDto
                {
                    PlantName = plant.Name,
                    TotalBars = plantBars.Count,
                    ReadyToShipBars = plantBars.Count(x => x.CurrentStatus == BarStatus.ReadyToShip),
                    ShippedBars = plantBars.Count(x => x.CurrentStatus == BarStatus.Shipped),
                    ReturnedBars = plantBars.Count(x => x.CurrentStatus == BarStatus.Returned),
                    DisposedBars = plantBars.Count(x => x.IsDisposed || x.CurrentStatus == BarStatus.Disposed)
                });
            }

            summary.BarsByStatus.Add(new BarStatusSummaryDto { StatusName = "Creadas", Count = summary.CreatedBars });
            summary.BarsByStatus.Add(new BarStatusSummaryDto { StatusName = "Aprobadas", Count = summary.ApprovedBars });
            summary.BarsByStatus.Add(new BarStatusSummaryDto { StatusName = "Rechazadas", Count = summary.RejectedBars });
            summary.BarsByStatus.Add(new BarStatusSummaryDto { StatusName = "Listas para envío", Count = summary.ReadyToShipBars });
            summary.BarsByStatus.Add(new BarStatusSummaryDto { StatusName = "Enviadas", Count = summary.ShippedBars });
            summary.BarsByStatus.Add(new BarStatusSummaryDto { StatusName = "Retornadas", Count = summary.ReturnedBars });
            summary.BarsByStatus.Add(new BarStatusSummaryDto { StatusName = "Dadas de baja", Count = summary.DisposedBars });

            return summary;
        }
    }
}