using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
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

        public QualityInspectionService(
            IRepository<Bar> barRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            IRepository<BarRecoveryPolicy> policyRepository,
            IRepository<QualityInspection> inspectionRepository,
            ICurrentUserService currentUserService,
            IAuditLogService auditLogService)
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

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));

            _auditLogService = auditLogService
                ?? throw new ArgumentNullException(nameof(auditLogService));
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

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var normalizedSearch = searchText.Trim().ToUpperInvariant();

                query = query.Where(x =>
                    !string.IsNullOrWhiteSpace(x.BarNumber) &&
                    x.BarNumber.ToUpperInvariant().Contains(normalizedSearch));
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
            string? notes)
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

            await _auditLogService.WriteAsync(
                AuditActionCodes.QualityInspectionCreated,
                "QualityInspection",
                inspection.Id,
                $"Se registro inspeccion de calidad para la barra {bar.BarNumber}.",
                BuildInspectionMetadataJson(
                    bar,
                    inspection,
                    recoveryCountAtInspection,
                    canBeRecovered,
                    mustBeDisposed,
                    isApprovedForShipment,
                    notes));

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
                                QualityInspection inspection,
                                int recoveryCountAtInspection,
                                bool canBeRecovered,
                                bool mustBeDisposed,
                                bool isApprovedForShipment,
                                string? notes)
        {
            var safeNotes = string.IsNullOrWhiteSpace(notes)
                ? string.Empty
                : notes.Trim().Replace("\"", "'");

            return
                "{" +
                $"\"BarId\":\"{bar.Id}\"," +
                $"\"BarNumber\":\"{bar.BarNumber}\"," +
                $"\"InspectionId\":\"{inspection.Id}\"," +
                $"\"RecoveryCountAtInspection\":{recoveryCountAtInspection}," +
                $"\"CanBeRecovered\":{canBeRecovered.ToString().ToLowerInvariant()}," +
                $"\"MustBeDisposed\":{mustBeDisposed.ToString().ToLowerInvariant()}," +
                $"\"IsApprovedForShipment\":{isApprovedForShipment.ToString().ToLowerInvariant()}," +
                $"\"ResultingStatus\":\"{bar.CurrentStatus}\"," +
                $"\"IsDisposed\":{bar.IsDisposed.ToString().ToLowerInvariant()}," +
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
    }
}