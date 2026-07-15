using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;
using BarRecoveryApp.ApplicationF.Services.Auditing;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class RecoveryWorkReportService : IRecoveryWorkReportService
    {
        private readonly IRepository<RecoveryWorkReport> _reportRepository;
        private readonly IRepository<RecoveryWorkReportCategory> _categoryRepository;
        private readonly IRepository<RecoveryWorkActivity> _activityReportRepository;
        private readonly IRepository<RecoveryWorkSupply> _supplyReportRepository;

        private readonly IRepository<ActivityModel> _activityRepository;
        private readonly IRepository<Supply> _supplyRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;

        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditLogService _auditLogService;


        public RecoveryWorkReportService(
            IRepository<RecoveryWorkReport> reportRepository,
            IRepository<RecoveryWorkReportCategory> categoryRepository,
            IRepository<RecoveryWorkActivity> activityReportRepository,
            IRepository<RecoveryWorkSupply> supplyReportRepository,
            IRepository<ActivityModel> activityRepository,
            IRepository<Supply> supplyRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            ICurrentUserService currentUserService,
            IAuditLogService auditLogService)
        {
            _reportRepository = reportRepository
                ?? throw new ArgumentNullException(nameof(reportRepository));

            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));

            _activityReportRepository = activityReportRepository
                ?? throw new ArgumentNullException(nameof(activityReportRepository));

            _supplyReportRepository = supplyReportRepository
                ?? throw new ArgumentNullException(nameof(supplyReportRepository));

            _activityRepository = activityRepository
                ?? throw new ArgumentNullException(nameof(activityRepository));

            _supplyRepository = supplyRepository
                ?? throw new ArgumentNullException(nameof(supplyRepository));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));

            _auditLogService = auditLogService
                ?? throw new ArgumentNullException(nameof(auditLogService));
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

        public async Task<List<RecoveryWorkReport>> GetMyReportsAsync()
        {
            var session = _currentUserService.CurrentSession;

            if (session is null)
                return new List<RecoveryWorkReport>();

            var reports = await _reportRepository.WhereAsync(
                x => x.UserId == session.UserId && x.IsActive);

            return reports
                .OrderByDescending(x => x.WorkDate)
                .ToList();
        }

        public async Task<bool> CreateReportAsync(
            DateTime workDate,
            string? shiftName,
            string? notes,
            List<RecoveryWorkCategoryInput> categories)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("RECOVERY_CREATE"))
                return false;

            var session = _currentUserService.CurrentSession;

            if (session is null)
                return false;

            if (categories is null || categories.Count == 0)
                return false;

            var isValid = await ValidateCategoriesAsync(categories);

            if (!isValid)
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

            foreach (var categoryInput in categories)
            {
                await InsertCategoryAsync(reportId, categoryInput);
            }

            var totalBarsWorked = categories.Sum(x => x.BarsWorkedCount);
            var totalActivities = categories.Sum(x => x.Activities.Count);
            var totalSupplies = categories.Sum(x => x.Supplies?.Count ?? 0);

            await _auditLogService.WriteAsync(
                AuditActionCodes.RecoveryReportCreated,
                "RecoveryWorkReport",
                reportId,
                $"Se creo un registro de recuperacion con {categories.Count} bloque(s), {totalBarsWorked} barra(s), {totalActivities} actividad(es) y {totalSupplies} insumo(s).",
                $"{{\"WorkDate\":\"{workDate:yyyy-MM-dd HH:mm:ss}\",\"ShiftName\":\"{shiftName}\",\"CategoryCount\":{categories.Count},\"TotalBarsWorked\":{totalBarsWorked},\"TotalActivities\":{totalActivities},\"TotalSupplies\":{totalSupplies}}}");

            return true;
        }

        public async Task<List<RecoveryWorkReportItemDto>> GetMyReportItemsAsync()
        {
            var session = _currentUserService.CurrentSession;

            if (session is null)
                return new List<RecoveryWorkReportItemDto>();

            var reports = await _reportRepository.WhereAsync(
                x => x.UserId == session.UserId && x.IsActive);

            var reportList = reports
                .OrderByDescending(x => x.WorkDate)
                .ToList();

            var allCategories = await _categoryRepository.GetAllAsync();
            var allReportActivities = await _activityReportRepository.GetAllAsync();
            var allReportSupplies = await _supplyReportRepository.GetAllAsync();

            var catalogActivities = await _activityRepository.GetAllAsync();
            var catalogSupplies = await _supplyRepository.GetAllAsync();

            var result = new List<RecoveryWorkReportItemDto>();

            foreach (var report in reportList)
            {
                var item = BuildReportItem(
                    report,
                    allCategories,
                    allReportActivities,
                    allReportSupplies,
                    catalogActivities,
                    catalogSupplies);

                result.Add(item);
            }

            return result;
        }

        private async Task<bool> ValidateCategoriesAsync(
            List<RecoveryWorkCategoryInput> categories)
        {
            var plants = await _plantRepository.GetActiveAsync();
            var barTypes = await _barTypeRepository.GetActiveAsync();
            var activities = await _activityRepository.GetActiveAsync();
            var supplies = await _supplyRepository.GetActiveAsync();

            foreach (var category in categories)
            {
                if (category.BarsWorkedCount <= 0)
                    return false;

                if (string.IsNullOrWhiteSpace(category.PlantId))
                    return false;

                if (string.IsNullOrWhiteSpace(category.BarTypeId))
                    return false;

                var plantExists = plants.Any(x => x.Id == category.PlantId);

                if (!plantExists)
                    return false;

                var barTypeExists = barTypes.Any(x => x.Id == category.BarTypeId);

                if (!barTypeExists)
                    return false;

                if (category.Activities is null || category.Activities.Count == 0)
                    return false;

                foreach (var activity in category.Activities)
                {
                    if (string.IsNullOrWhiteSpace(activity.ActivityId))
                        return false;

                    if (activity.HoursWorked <= 0)
                        return false;

                    var activityExists = activities.Any(x => x.Id == activity.ActivityId);

                    if (!activityExists)
                        return false;
                }

                if (category.Supplies is not null)
                {
                    foreach (var supply in category.Supplies)
                    {
                        if (string.IsNullOrWhiteSpace(supply.SupplyId))
                            return false;

                        if (supply.Quantity <= 0)
                            return false;

                        var supplyExists = supplies.Any(x => x.Id == supply.SupplyId);

                        if (!supplyExists)
                            return false;
                    }
                }
            }

            return true;
        }

        private async Task InsertCategoryAsync(
            string reportId,
            RecoveryWorkCategoryInput categoryInput)
        {
            var plant = await _plantRepository.GetByIdAsync(categoryInput.PlantId);
            var barType = await _barTypeRepository.GetByIdAsync(categoryInput.BarTypeId);

            if (plant is null)
                throw new InvalidOperationException("No se encontró la planta asociada a la categoría.");

            if (barType is null)
                throw new InvalidOperationException("No se encontró el tipo de barra asociado a la categoría.");

            var categoryId = Guid.NewGuid().ToString();

            var category = new RecoveryWorkReportCategory
            {
                Id = categoryId,
                RecoveryWorkReportId = reportId,
                WorkType = categoryInput.WorkType,
                PlantId = plant.Id,
                BarTypeId = barType.Id,
                BarsWorkedCount = categoryInput.BarsWorkedCount,
                ExportLabel = BuildExportLabel(
                    categoryInput.WorkType,
                    plant.Name,
                    barType.Name),
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await _categoryRepository.InsertAsync(category);

            await InsertActivitiesAsync(categoryId, categoryInput.Activities);

            await InsertSuppliesAsync(categoryId, categoryInput.Supplies);
        }

        private async Task InsertActivitiesAsync(
            string categoryId,
            List<RecoveryWorkActivityInput> activities)
        {
            foreach (var activity in activities)
            {
                var item = new RecoveryWorkActivity
                {
                    Id = Guid.NewGuid().ToString(),
                    RecoveryWorkReportCategoryId = categoryId,
                    ActivityId = activity.ActivityId,
                    HoursWorked = activity.HoursWorked,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _activityReportRepository.InsertAsync(item);
            }
        }

        private async Task InsertSuppliesAsync(
            string categoryId,
            List<RecoveryWorkSupplyInput>? supplies)
        {
            if (supplies is null || supplies.Count == 0)
                return;

            foreach (var supply in supplies)
            {
                var item = new RecoveryWorkSupply
                {
                    Id = Guid.NewGuid().ToString(),
                    RecoveryWorkReportCategoryId = categoryId,
                    SupplyId = supply.SupplyId,
                    Quantity = supply.Quantity,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _supplyReportRepository.InsertAsync(item);
            }
        }

        private static RecoveryWorkReportItemDto BuildReportItem(
            RecoveryWorkReport report,
            List<RecoveryWorkReportCategory> allCategories,
            List<RecoveryWorkActivity> allReportActivities,
            List<RecoveryWorkSupply> allReportSupplies,
            List<ActivityModel> catalogActivities,
            List<Supply> catalogSupplies)
        {
            var item = new RecoveryWorkReportItemDto
            {
                Id = report.Id,
                WorkDate = report.WorkDate,
                ShiftName = report.ShiftName,
                Notes = report.Notes
            };

            var reportCategories = allCategories
                .Where(x => x.RecoveryWorkReportId == report.Id && x.IsActive)
                .OrderBy(x => x.ExportLabel)
                .ToList();

            foreach (var category in reportCategories)
            {
                var categoryDto = BuildCategoryDetail(
                    category,
                    allReportActivities,
                    allReportSupplies,
                    catalogActivities,
                    catalogSupplies);

                item.Categories.Add(categoryDto);
            }

            return item;
        }

        private static RecoveryWorkCategoryDetailDto BuildCategoryDetail(
            RecoveryWorkReportCategory category,
            List<RecoveryWorkActivity> allReportActivities,
            List<RecoveryWorkSupply> allReportSupplies,
            List<ActivityModel> catalogActivities,
            List<Supply> catalogSupplies)
        {
            var categoryDto = new RecoveryWorkCategoryDetailDto
            {
                ExportLabel = category.ExportLabel,
                BarsWorkedCount = category.BarsWorkedCount
            };

            var activities = allReportActivities
                .Where(x => x.RecoveryWorkReportCategoryId == category.Id && x.IsActive)
                .ToList();

            foreach (var activity in activities)
            {
                var catalogActivity = catalogActivities
                    .FirstOrDefault(x => x.Id == activity.ActivityId);

                categoryDto.Activities.Add(new RecoveryWorkActivityDetailDto
                {
                    ActivityId = activity.ActivityId,
                    ActivityName = catalogActivity?.Name ?? "Actividad no encontrada",
                    HoursWorked = activity.HoursWorked
                });
            }

            var supplies = allReportSupplies
                .Where(x => x.RecoveryWorkReportCategoryId == category.Id && x.IsActive)
                .ToList();

            foreach (var supply in supplies)
            {
                var catalogSupply = catalogSupplies
                    .FirstOrDefault(x => x.Id == supply.SupplyId);

                categoryDto.Supplies.Add(new RecoveryWorkSupplyDetailDto
                {
                    SupplyId = supply.SupplyId,
                    SupplyName = catalogSupply?.Name ?? "Insumo no encontrado",
                    Unit = catalogSupply?.Unit ?? string.Empty,
                    Quantity = supply.Quantity
                });
            }

            return categoryDto;
        }

        private static string BuildExportLabel(
            ProductionWorkType workType,
            string plantName,
            string barTypeName)
        {
            var prefix = workType == ProductionWorkType.Recovery
                ? "Recuperación"
                : "Fabricación";

            return $"{prefix} {plantName} - {barTypeName}";
        }
    }
}