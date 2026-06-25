using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<List<User>> GetActiveUsersAsync();

        Task<AuthResult> LoginWithPinAsync(string userId, string pin);

        Task<bool> ChangePinAsync(string userId, string currentPin, string newPin);

        Task<bool> ResetPinAsync(string userId, string newPin, string performedByUserId);

        Task LogoutAsync();
    }
}
