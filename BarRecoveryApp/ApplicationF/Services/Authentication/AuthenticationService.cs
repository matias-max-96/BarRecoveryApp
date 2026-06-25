using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Infrastructure.Persistence.Seed;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {

        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<Permission> _permissionRepository;
        private readonly IRepository<RolePermission> _rolePermissionRepository;
        private readonly ICurrentUserService _currentUserService;


        public AuthenticationService(
                    IRepository<User> userRepository,
                    IRepository<Role> roleRepository,
                    IRepository<Permission> permissionRepository,
                    IRepository<RolePermission> rolePermissionRepository,
                    ICurrentUserService currentUserService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _currentUserService = currentUserService;
        }

        public async Task<List<User>> GetActiveUsersAsync()
        {
            var users = await _userRepository.GetActiveAsync();

            return users
                .OrderBy(x => x.DisplayName)
                .ToList();
        }

        public async Task<AuthResult> LoginWithPinAsync(string userId, string pin)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return AuthResult.Fail("Debe seleccionar un usuario.");

            if (string.IsNullOrWhiteSpace(pin))
                return AuthResult.Fail("Debe ingresar el PIN.");

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                return AuthResult.Fail("Usuario no encontrado.");

            if (!user.IsActive)
                return AuthResult.Fail("El usuario se encuentra inactivo.");

            if (!user.IsPinEnabled)
                return AuthResult.Fail("El ingreso por PIN se encuentra deshabilitado para este usuario.");

            if (string.IsNullOrWhiteSpace(user.PinHash) || string.IsNullOrWhiteSpace(user.PinSalt))
                return AuthResult.Fail("El usuario no tiene PIN configurado.");

            bool isValidPin = PinHashHelper.VerifyPin(
                pin,
                user.PinHash,
                user.PinSalt);

            if (!isValidPin)
                return AuthResult.Fail("PIN incorrecto.");

            var role = await _roleRepository.GetByIdAsync(user.RoleId);

            if (role is null)
                return AuthResult.Fail("El usuario no tiene un rol válido.");

            if (!role.IsActive)
                return AuthResult.Fail("El rol del usuario se encuentra inactivo.");

            var permissions = await GetPermissionsByRoleAsync(role.Id);

            var session = new CurrentUserSession
            {
                UserId = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                RoleId = role.Id,
                RoleCode = role.Code,
                RoleName = role.Name,
                PermissionCodes = permissions
                    .Where(x => x.IsActive)
                    .Select(x => x.Code)
                    .Distinct()
                    .ToList(),
                LoginAt = DateTime.Now,
                LastActivityAt = DateTime.Now,
                IsAuthenticated = true,
                MustChangePin = user.MustChangePin
            };

            _currentUserService.SetSession(session);

            return AuthResult.Ok(
                user,
                role,
                permissions,
                user.MustChangePin);
        }

        public async Task<bool> ChangePinAsync(
            string userId,
            string currentPin,
            string newPin)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            if (string.IsNullOrWhiteSpace(currentPin))
                return false;

            if (string.IsNullOrWhiteSpace(newPin))
                return false;

            if (!IsValidPinFormat(newPin))
                return false;

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                return false;

            if (!user.IsActive)
                return false;

            bool currentPinIsValid = PinHashHelper.VerifyPin(
                currentPin,
                user.PinHash,
                user.PinSalt);

            if (!currentPinIsValid)
                return false;

            var newPinHash = PinHashHelper.CreateHash(newPin);

            user.PinHash = newPinHash.Hash;
            user.PinSalt = newPinHash.Salt;
            user.MustChangePin = false;
            user.UpdatedAtUtc = DateTime.Now;

            await _userRepository.UpdateAsync(user);

            if (_currentUserService.CurrentSession is not null &&
                _currentUserService.CurrentSession.UserId == user.Id)
            {
                _currentUserService.CurrentSession.MustChangePin = false;
                _currentUserService.UpdateActivity();
            }

            return true;
        }

        public async Task<bool> ResetPinAsync(
            string userId,
            string newPin,
            string performedByUserId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            if (string.IsNullOrWhiteSpace(newPin))
                return false;

            if (!IsValidPinFormat(newPin))
                return false;

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                return false;

            if (!user.IsActive)
                return false;

            var newPinHash = PinHashHelper.CreateHash(newPin);

            user.PinHash = newPinHash.Hash;
            user.PinSalt = newPinHash.Salt;
            user.MustChangePin = true;
            user.UpdatedByUserId = performedByUserId;
            user.UpdatedAtUtc = DateTime.Now;

            await _userRepository.UpdateAsync(user);

            return true;
        }

        public Task LogoutAsync()
        {
            _currentUserService.ClearSession();

            return Task.CompletedTask;
        }

        private async Task<List<Permission>> GetPermissionsByRoleAsync(string roleId)
        {
            var rolePermissions = await _rolePermissionRepository
                .WhereAsync(x => x.RoleId == roleId && x.IsActive);

            if (rolePermissions.Count == 0)
                return new List<Permission>();

            var permissions = new List<Permission>();

            foreach (var rolePermission in rolePermissions)
            {
                var permission = await _permissionRepository.GetByIdAsync(
                    rolePermission.PermissionId);

                if (permission is null)
                    continue;

                if (!permission.IsActive)
                    continue;

                permissions.Add(permission);
            }

            return permissions;
        }

        private static bool IsValidPinFormat(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                return false;

            if (pin.Length < 4)
                return false;

            if (pin.Length > 6)
                return false;

            return pin.All(char.IsDigit);
        }

    }
}
