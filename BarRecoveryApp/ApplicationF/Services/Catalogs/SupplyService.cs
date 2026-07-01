using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    public class SupplyService : ISupplyService
    {
        private readonly IRepository<Supply> _supplyRepository;
        private readonly ICurrentUserService _currentUserService;

        public SupplyService(
            IRepository<Supply> supplyRepository,
            ICurrentUserService currentUserService)
        {
            _supplyRepository = supplyRepository
                ?? throw new ArgumentNullException(nameof(supplyRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<Supply>> GetSuppliesAsync()
        {
            var supplies = await _supplyRepository.GetAllAsync();

            return supplies
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<bool> SaveSupplyAsync(
            string? supplyId,
            string code,
            string name,
            string unit,
            string? description)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("SUPPLY_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (string.IsNullOrWhiteSpace(unit))
                return false;

            var normalizedCode = code.Trim().ToUpperInvariant();
            var normalizedName = name.Trim();
            var normalizedUnit = unit.Trim();
            var normalizedDescription = description?.Trim();

            var existing = await _supplyRepository.FirstOrDefaultAsync(
                x => x.Code == normalizedCode);

            if (existing is not null &&
                existing.Id != supplyId)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(supplyId))
            {
                var supply = new Supply
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = normalizedCode,
                    Name = normalizedName,
                    Unit = normalizedUnit,
                    Description = normalizedDescription,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _supplyRepository.InsertAsync(supply);

                return true;
            }
            else
            {
                var supply = await _supplyRepository.GetByIdAsync(supplyId);

                if (supply is null)
                    return false;

                supply.Code = normalizedCode;
                supply.Name = normalizedName;
                supply.Unit = normalizedUnit;
                supply.Description = normalizedDescription;
                supply.UpdatedAtUtc = DateTime.Now;

                await _supplyRepository.UpdateAsync(supply);

                return true;
            }
        }

        public async Task<bool> SetSupplyActiveStateAsync(
            string supplyId,
            bool isActive)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!_currentUserService.HasPermission("SUPPLY_MANAGE"))
                return false;

            if (string.IsNullOrWhiteSpace(supplyId))
                return false;

            var supply = await _supplyRepository.GetByIdAsync(supplyId);

            if (supply is null)
                return false;

            supply.IsActive = isActive;
            supply.UpdatedAtUtc = DateTime.Now;

            await _supplyRepository.UpdateAsync(supply);

            return true;
        }
    }
}