using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public class BarTypeService : IBarTypeService
    {
        private readonly IRepository<BarType> _barTypeRepository;
        private readonly ICurrentUserService _currentUserService;

        public BarTypeService(
            IRepository<BarType> barTypeRepository,
            ICurrentUserService currentUserService)
        {
            _barTypeRepository = barTypeRepository
                ?? throw new ArgumentNullException(nameof(barTypeRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<BarType>> GetBarTypesAsync()
        {
            var barTypes = await _barTypeRepository.GetAllAsync();

            return barTypes
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<bool> SaveBarTypeAsync(
            string? barTypeId,
            string code,
            string name,
            string? description)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("BAR_TYPE_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalizedCode = code.Trim().ToUpperInvariant();
            var normalizedName = name.Trim();
            var normalizedDescription = description?.Trim();

            var existing = await _barTypeRepository.FirstOrDefaultAsync(
                x => x.Code == normalizedCode);

            if (existing is not null &&
                existing.Id != barTypeId)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(barTypeId))
            {
                var barType = new BarType
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = normalizedCode,
                    Name = normalizedName,
                    Description = normalizedDescription,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _barTypeRepository.InsertAsync(barType);

                return true;
            }
            else
            {
                var barType = await _barTypeRepository.GetByIdAsync(barTypeId);

                if (barType is null)
                    return false;

                barType.Code = normalizedCode;
                barType.Name = normalizedName;
                barType.Description = normalizedDescription;
                barType.UpdatedAtUtc = DateTime.Now;

                await _barTypeRepository.UpdateAsync(barType);

                return true;
            }
        }

        public async Task<bool> SetBarTypeActiveStateAsync(
            string barTypeId,
            bool isActive)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("BAR_TYPE_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(barTypeId))
                return false;

            var barType = await _barTypeRepository.GetByIdAsync(barTypeId);

            if (barType is null)
                return false;

            barType.IsActive = isActive;
            barType.UpdatedAtUtc = DateTime.Now;

            await _barTypeRepository.UpdateAsync(barType);

            return true;
        }
    }
}