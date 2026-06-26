using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Infrastructure.Persistence.Seed;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Users
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly ICurrentUserService _currentUserService;

        private const string SuperAdminRoleCode = "SUPER_ADMIN";
        private const string AdminRoleCode = "ADMIN";

        public UserManagementService(
            IRepository<User> userRepository,
            IRepository<Role> roleRepository,
            ICurrentUserService currentUserService)
        {
            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));

            _roleRepository = roleRepository
                ?? throw new ArgumentNullException(nameof(roleRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<User>> GetUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();

            return users
                .OrderBy(x => x.DisplayName)
                .ToList();
        }

        public async Task<List<Role>> GetAvailableRolesForCurrentUserAsync()
        {
            var roles = await _roleRepository.GetActiveAsync();

            var currentSession = _currentUserService.CurrentSession;

            if (currentSession is null)
                return new List<Role>();

            if (currentSession.RoleCode == SuperAdminRoleCode)
            {
                return roles
                    .OrderBy(x => x.Name)
                    .ToList();
            }

            if (currentSession.RoleCode == AdminRoleCode)
            {
                return roles
                    .Where(x => x.Code != SuperAdminRoleCode)
                    .OrderBy(x => x.Name)
                    .ToList();
            }

            return new List<Role>();
        }

        public async Task<bool> CreateUserAsync(
    string username,
    string displayName,
    string roleId,
    string initialPin)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (string.IsNullOrWhiteSpace(username))
                return false;

            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            if (string.IsNullOrWhiteSpace(roleId))
                return false;

            if (!IsValidPin(initialPin))
                return false;

            var currentSession = _currentUserService.CurrentSession;

            if (currentSession is null)
                return false;

            var normalizedUsername = username.Trim();
            var normalizedDisplayName = displayName.Trim();

            var role = await _roleRepository.GetByIdAsync(roleId);

            if (role is null || !role.IsActive)
                return false;

            if (currentSession.RoleCode != SuperAdminRoleCode &&
                role.Code == SuperAdminRoleCode)
            {
                return false;
            }

            var existingUser = await _userRepository.FirstOrDefaultAsync(
                x => x.Username == normalizedUsername);

            if (existingUser is not null)
                return false;

            var pin = PinHashHelper.CreateHash(initialPin);

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = normalizedUsername,
                DisplayName = normalizedDisplayName,
                RoleId = role.Id,
                PinHash = pin.Hash,
                PinSalt = pin.Salt,
                IsPinEnabled = true,
                MustChangePin = true,
                IsActive = true,
                CreatedByUserId = currentSession.UserId,
                UpdatedByUserId = null,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await _userRepository.InsertAsync(user);

            return true;
        }

        public async Task<bool> SetUserActiveStateAsync(
            string userId,
            bool isActive)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            var currentSession = _currentUserService.CurrentSession;

            if (currentSession is null)
                return false;

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                return false;

            var userRole = await _roleRepository.GetByIdAsync(user.RoleId);

            if (userRole is null)
                return false;

            if (userRole.Code == SuperAdminRoleCode &&
                currentSession.RoleCode != SuperAdminRoleCode)
            {
                return false;
            }

            if (user.Id == currentSession.UserId && !isActive)
            {
                return false;
            }

            user.IsActive = isActive;
            user.UpdatedByUserId = currentSession.UserId;
            user.UpdatedAtUtc = DateTime.Now;

            await _userRepository.UpdateAsync(user);

            return true;
        }

        public async Task<bool> ResetUserPinAsync(
            string userId,
            string newPin)
        {
            if (!_currentUserService.IsAuthenticated)
                return false;

            if (!IsValidPin(newPin))
                return false;

            var currentSession = _currentUserService.CurrentSession;

            if (currentSession is null)
                return false;

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                return false;

            var userRole = await _roleRepository.GetByIdAsync(user.RoleId);

            if (userRole is null)
                return false;

            if (userRole.Code == SuperAdminRoleCode &&
                currentSession.RoleCode != SuperAdminRoleCode)
            {
                return false;
            }

            var pin = PinHashHelper.CreateHash(newPin);

            user.PinHash = pin.Hash;
            user.PinSalt = pin.Salt;
            user.MustChangePin = true;
            user.UpdatedByUserId = currentSession.UserId;
            user.UpdatedAtUtc = DateTime.Now;

            await _userRepository.UpdateAsync(user);

            return true;
        }

        private static bool IsValidPin(string pin)
        {
            return !string.IsNullOrWhiteSpace(pin)
                   && pin.Length >= 4
                   && pin.Length <= 6
                   && pin.All(char.IsDigit);
        }
    }
}