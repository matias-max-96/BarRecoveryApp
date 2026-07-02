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
        //private readonly IRepository<RecoveryRecord> _recoveryRecordRepository;
        //private readonly IRepository<RecoveryRecordSupply> _recoveryRecordSupplyRepository;
        private readonly ICurrentUserService _currentUserService;

        public RecoveryService(
            IRepository<Bar> barRepository,
            IRepository<ActivityModel> activityRepository,
            IRepository<Supply> supplyRepository,
            //IRepository<RecoveryRecord> recoveryRecordRepository,
            //IRepository<RecoveryRecordSupply> recoveryRecordSupplyRepository,
            ICurrentUserService currentUserService)
        {
            _barRepository = barRepository
                ?? throw new ArgumentNullException(nameof(barRepository));

            _activityRepository = activityRepository
                ?? throw new ArgumentNullException(nameof(activityRepository));

            _supplyRepository = supplyRepository
                ?? throw new ArgumentNullException(nameof(supplyRepository));

            /*_recoveryRecordRepository = recoveryRecordRepository
                ?? throw new ArgumentNullException(nameof(recoveryRecordRepository));

            _recoveryRecordSupplyRepository = recoveryRecordSupplyRepository
                ?? throw new ArgumentNullException(nameof(recoveryRecordSupplyRepository));*/

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

            /*var recoveryRecord = new RecoveryRecord
            {
                Id = Guid.NewGuid().ToString(),
                BarId = bar.Id,
                UserId = session.UserId,
                ActivityId = activity.Id,
                PerformedAtUtc = DateTime.Now,
                Notes = notes?.Trim(),
                DeviceId = "LOCAL_DEVICE",
                SyncStatus = SyncStatus.Pending,
                RemoteId = null,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await _recoveryRecordRepository.InsertAsync(recoveryRecord);*/

            foreach (var supplyInput in supplies)
            {
                /*var recordSupply = new RecoveryRecordSupply
                {
                    Id = Guid.NewGuid().ToString(),
                    RecoveryRecordId = recoveryRecord.Id,
                    SupplyId = supplyInput.SupplyId,
                    Quantity = supplyInput.Quantity,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _recoveryRecordSupplyRepository.InsertAsync(recordSupply);*/
            }

            bar.CurrentStatus = BarStatus.PendingQuality;
            bar.UpdatedAtUtc = DateTime.Now;

            await _barRepository.UpdateAsync(bar);

            return true;
        }
    }
}