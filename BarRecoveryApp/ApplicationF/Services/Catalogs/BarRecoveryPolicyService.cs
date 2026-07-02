using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public class BarRecoveryPolicyService : IBarRecoveryPolicyService
    {
        private readonly IRepository<BarRecoveryPolicy> _policyRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;

        public BarRecoveryPolicyService(
            IRepository<BarRecoveryPolicy> policyRepository,
            IRepository<Plant> plantRepository,
            IRepository<BarType> barTypeRepository,
            ICurrentUserService currentUserService)
        {
            _policyRepository = policyRepository
                ?? throw new ArgumentNullException(nameof(policyRepository));

            _plantRepository = plantRepository
                ?? throw new ArgumentNullException(nameof(plantRepository));

            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<BarRecoveryPolicy>> GetPoliciesAsync()
        {
            var policies = await _policyRepository.GetAllAsync();

            return policies
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.CreatedAtUtc)
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

        public async Task<bool> SavePolicyAsync(
            string? policyId,
            string plantId,
            string barTypeId,
            int maxRecoveries)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("POLICY_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(plantId))
                return false;

            if (string.IsNullOrWhiteSpace(barTypeId))
                return false;

            if (maxRecoveries < 0)
                return false;

            var plant = await _plantRepository.GetByIdAsync(plantId);

            if (plant is null || !plant.IsActive)
                return false;

            var barType = await _barTypeRepository.GetByIdAsync(barTypeId);

            if (barType is null || !barType.IsActive)
                return false;

            var existing = await _policyRepository.FirstOrDefaultAsync(
                x => x.PlantId == plantId && x.BarTypeId == barTypeId);

            if (existing is not null &&
                existing.Id != policyId)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(policyId))
            {
                var policy = new BarRecoveryPolicy
                {
                    Id = Guid.NewGuid().ToString(),
                    PlantId = plantId,
                    BarTypeId = barTypeId,
                    MaxRecoveries = maxRecoveries,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _policyRepository.InsertAsync(policy);

                return true;
            }
            else
            {
                var policy = await _policyRepository.GetByIdAsync(policyId);

                if (policy is null)
                    return false;

                policy.PlantId = plantId;
                policy.BarTypeId = barTypeId;
                policy.MaxRecoveries = maxRecoveries;
                policy.UpdatedAtUtc = DateTime.Now;

                await _policyRepository.UpdateAsync(policy);

                return true;
            }
        }

        public async Task<bool> SetPolicyActiveStateAsync(
            string policyId,
            bool isActive)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("POLICY_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(policyId))
                return false;

            var policy = await _policyRepository.GetByIdAsync(policyId);

            if (policy is null)
                return false;

            policy.IsActive = isActive;
            policy.UpdatedAtUtc = DateTime.Now;

            await _policyRepository.UpdateAsync(policy);

            return true;
        }
    }
}