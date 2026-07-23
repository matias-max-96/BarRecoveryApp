namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public interface ICentralApiAuthClient
    {
        Task<string?> GetValidTokenAsync();

        void InvalidateCachedToken();
    }
}