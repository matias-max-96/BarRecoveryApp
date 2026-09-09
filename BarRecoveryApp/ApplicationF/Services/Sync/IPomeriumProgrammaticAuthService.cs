namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public class PomeriumSignInResult
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        public static PomeriumSignInResult Ok() =>
            new() { Success = true };

        public static PomeriumSignInResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    public interface IPomeriumProgrammaticAuthService
    {
        Task<string?> GetStoredTokenAsync();

        Task<PomeriumSignInResult> SignInAsync();

        Task ClearStoredTokenAsync();
    }
}