using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;


namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class RecoveryWorkReportService : IRecoveryWorkReportService
    {
        private readonly IRepository<RecoveryWorkReport> _reportRepository;
        private readonly IRepository<RecoveryWorkActivity> _activityReportRepository;
        private readonly IRepository<RecoveryWorkSupply> _supplyReportRepository;
        private readonly IRepository<ActivityModel> _activityRepository;
        private readonly IRepository<Supply> _supplyRepository;
        private readonly ICurrentUserService _currentUserService;

        public RecoveryWorkReportService(
            IRepository<RecoveryWorkReport> reportRepository,
            IRepository<RecoveryWorkActivity> activityReportRepository,
            IRepository<RecoveryWorkSupply> supplyReportRepository,
            IRepository<ActivityModel> activityRepository,
            IRepository<Supply> supplyRepository,
            ICurrentUserService currentUserService)
        {
            _reportRepository = reportRepository
                ?? throw new ArgumentNullException(nameof(reportRepository));

            _activityReportRepository = activityReportRepository
                ?? throw new ArgumentNullException(nameof(activityReportRepository));

            _supplyReportRepository = supplyReportRepository
                ?? throw new ArgumentNullException(nameof(supplyReportRepository));

            _activityRepository = activityRepository
                ?? throw new ArgumentNullException(nameof(activityRepository));

            _supplyRepository = supplyRepository
                ?? throw new ArgumentNullException(nameof(supplyRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<ActivityModel>> GetActiveActivitiesAsync()
        {
            var activities = await _activityRepository.GetActiveAsync();

            return activities
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<List<Supply>> GetActiveSuppliesAsync()
        {
            var supplies = await _supplyRepository.GetActiveAsync();

            return supplies
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<List<RecoveryWorkReport>> GetMyReportsAsync()
        {
            var session = _currentUserService.CurrentSession;

            if (session is null)
                return new List<RecoveryWorkReport>();

            var reports = await _reportRepository.WhereAsync(
                x => x.UserId == session.UserId);

            return reports
                .OrderByDescending(x => x.WorkDate)
                .ToList();
        }

        public async Task<bool> CreateReportAsync(
            DateTime workDate,
            string? shiftName,
            int barsWorkedCount,
            string? notes,
            List<RecoveryWorkActivityInput> activities,
            List<RecoveryWorkSupplyInput> supplies)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("RECOVERY_CREATE"))
                return false;

            var session = _currentUserService.CurrentSession;

            if (session is null)
                return false;

            if (barsWorkedCount <= 0)
                return false;

            if (activities is null || activities.Count == 0)
                return false;

            if (activities.Any(x => string.IsNullOrWhiteSpace(x.ActivityId) || x.HoursWorked <= 0))
                return false;

            if (supplies is not null &&
                supplies.Any(x => string.IsNullOrWhiteSpace(x.SupplyId) || x.Quantity <= 0))
                return false;

            var reportId = Guid.NewGuid().ToString();

            var report = new RecoveryWorkReport
            {
                Id = reportId,
                UserId = session.UserId,
                WorkDate = workDate,
                ShiftName = string.IsNullOrWhiteSpace(shiftName)
                    ? null
                    : shiftName.Trim(),
                BarsWorkedCount = barsWorkedCount,
                Notes = string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim(),
                DeviceId = DeviceInfo.Current.Name ?? string.Empty,
                SyncStatus = SyncStatus.Pending,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await _reportRepository.InsertAsync(report);

            foreach (var activity in activities)
            {
                var item = new RecoveryWorkActivity
                {
                    Id = Guid.NewGuid().ToString(),
                    RecoveryWorkReportId = reportId,
                    ActivityId = activity.ActivityId,
                    HoursWorked = activity.HoursWorked,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _activityReportRepository.InsertAsync(item);
            }

            if (supplies is not null)
            {
                foreach (var supply in supplies)
                {
                    var item = new RecoveryWorkSupply
                    {
                        Id = Guid.NewGuid().ToString(),
                        RecoveryWorkReportId = reportId,
                        SupplyId = supply.SupplyId,
                        Quantity = supply.Quantity,
                        IsActive = true,
                        CreatedAtUtc = DateTime.Now,
                        UpdatedAtUtc = DateTime.Now
                    };

                    await _supplyReportRepository.InsertAsync(item);
                }
            }

            return true;
        }
        public async Task<List<RecoveryWorkReportItemDto>> GetMyReportItemsAsync()
        {
            var session = _currentUserService.CurrentSession;

            if (session is null)
                return new List<RecoveryWorkReportItemDto>();

            var reports = await _reportRepository.WhereAsync(
                x => x.UserId == session.UserId);

            var reportList = reports
                .OrderByDescending(x => x.WorkDate)
                .ToList();

            var allReportActivities = await _activityReportRepository.GetAllAsync();
            var allReportSupplies = await _supplyReportRepository.GetAllAsync();

            var catalogActivities = await _activityRepository.GetAllAsync();
            var catalogSupplies = await _supplyRepository.GetAllAsync();

            var result = new List<RecoveryWorkReportItemDto>();

            foreach (var report in reportList)
            {
                var reportActivities = allReportActivities
                    .Where(x => x.RecoveryWorkReportId == report.Id && x.IsActive)
                    .ToList();

                var reportSupplies = allReportSupplies
                    .Where(x => x.RecoveryWorkReportId == report.Id && x.IsActive)
                    .ToList();

                var item = new RecoveryWorkReportItemDto
                {
                    Id = report.Id,
                    WorkDate = report.WorkDate,
                    ShiftName = report.ShiftName,
                    BarsWorkedCount = report.BarsWorkedCount,
                    Notes = report.Notes
                };

                foreach (var activity in reportActivities)
                {
                    var catalogActivity = catalogActivities
                        .FirstOrDefault(x => x.Id == activity.ActivityId);

                    item.Activities.Add(new RecoveryWorkActivityDetailDto
                    {
                        ActivityId = activity.ActivityId,
                        ActivityName = catalogActivity?.Name ?? "Actividad no encontrada",
                        HoursWorked = activity.HoursWorked
                    });
                }

                foreach (var supply in reportSupplies)
                {
                    var catalogSupply = catalogSupplies
                        .FirstOrDefault(x => x.Id == supply.SupplyId);

                    item.Supplies.Add(new RecoveryWorkSupplyDetailDto
                    {
                        SupplyId = supply.SupplyId,
                        SupplyName = catalogSupply?.Name ?? "Insumo no encontrado",
                        Unit = catalogSupply?.Unit ?? string.Empty,
                        Quantity = supply.Quantity
                    });
                }

                result.Add(item);
            }

            return result;
        }
    }
}