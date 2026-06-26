using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Users
{
    public interface IUserManagementService
    {
        Task<List<User>> GetUsersAsync();

        Task<List<Role>> GetAvailableRolesForCurrentUserAsync();

        Task<bool> CreateUserAsync(
            string username,
            string displayName,
            string roleId,
            string initialPin);

        Task<bool> SetUserActiveStateAsync(
            string userId,
            bool isActive);

        Task<bool> ResetUserPinAsync(
            string userId,
            string newPin);
    }
}