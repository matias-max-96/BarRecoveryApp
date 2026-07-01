using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public class ActivityService : IActivityService
    {
        private readonly IRepository<ActivityModel> _activityRepository;
        private readonly ICurrentUserService _currentUserService;

        public ActivityService(
            IRepository<ActivityModel> activityRepository,
            ICurrentUserService currentUserService)
        {
            _activityRepository = activityRepository
                ?? throw new ArgumentNullException(nameof(activityRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<ActivityModel>> GetActivitiesAsync()
        {
            var activities = await _activityRepository.GetAllAsync();

            return activities
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<bool> SaveActivityAsync(
            string? activityId,
            string code,
            string name,
            string? description)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("ACTIVITY_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalizedCode = code.Trim().ToUpperInvariant();
            var normalizedName = name.Trim();
            var normalizedDescription = description?.Trim();

            var existing = await _activityRepository.FirstOrDefaultAsync(
                x => x.Code == normalizedCode);

            if (existing is not null &&
                existing.Id != activityId)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(activityId))
            {
                var activity = new ActivityModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = normalizedCode,
                    Name = normalizedName,
                    Description = normalizedDescription,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _activityRepository.InsertAsync(activity);

                return true;
            }
            else
            {
                var activity = await _activityRepository.GetByIdAsync(activityId);

                if (activity is null)
                    return false;

                activity.Code = normalizedCode;
                activity.Name = normalizedName;
                activity.Description = normalizedDescription;
                activity.UpdatedAtUtc = DateTime.Now;

                await _activityRepository.UpdateAsync(activity);

                return true;
            }
        }

        public async Task<bool> SetActivityActiveStateAsync(
            string activityId,
            bool isActive)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("ACTIVITY_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(activityId))
                return false;

            var activity = await _activityRepository.GetByIdAsync(activityId);

            if (activity is null)
                return false;

            activity.IsActive = isActive;
            activity.UpdatedAtUtc = DateTime.Now;

            await _activityRepository.UpdateAsync(activity);

            return true;
        }
    }
}