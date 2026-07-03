using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class RecoveryService : IRecoveryService
    {
        private readonly IRepository<Bar> _barRepository;
        private readonly IRepository<ActivityModel> _activityRepository;
        private readonly IRepository<Supply> _supplyRepository;
        private readonly ICurrentUserService _currentUserService;

        public RecoveryService(
            IRepository<Bar> barRepository,
            IRepository<ActivityModel> activityRepository,
            IRepository<Supply> supplyRepository,
            ICurrentUserService currentUserService)
        {
            _barRepository = barRepository
                ?? throw new ArgumentNullException(nameof(barRepository));

            _activityRepository = activityRepository
                ?? throw new ArgumentNullException(nameof(activityRepository));

            _supplyRepository = supplyRepository
                ?? throw new ArgumentNullException(nameof(supplyRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<Bar>> GetAvailableBarsAsync()
        {
            var bars = await _barRepository.GetActiveAsync();

            return bars
                .Where(x => !x.IsDisposed)
                .OrderBy(x => x.BarNumber)
                .ToList();
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

        public async Task<bool> RegisterRecoveryAsync(
            string barId,
            string activityId,
            string? notes,
            List<RecoverySupplyInput> supplies)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("RECOVERY_CREATE"))
                return false;

            var session = _currentUserService.CurrentSession;

            if (session is null)
                return false;

            if (string.IsNullOrWhiteSpace(barId))
                return false;

            if (string.IsNullOrWhiteSpace(activityId))
                return false;

            var bar = await _barRepository.GetByIdAsync(barId);

            if (bar is null || !bar.IsActive || bar.IsDisposed)
                return false;

            var activity = await _activityRepository.GetByIdAsync(activityId);

            if (activity is null || !activity.IsActive)
                return false;

            if (supplies.Any(x => x.Quantity <= 0))
                return false;

            foreach (var supplyInput in supplies)
            {
                var supply = await _supplyRepository.GetByIdAsync(supplyInput.SupplyId);

                if (supply is null || !supply.IsActive)
                    return false;
            }

            bar.CurrentStatus = BarStatus.PendingQuality;
            bar.UpdatedAtUtc = DateTime.Now;

            await _barRepository.UpdateAsync(bar);

            return true;
        }
    }
}