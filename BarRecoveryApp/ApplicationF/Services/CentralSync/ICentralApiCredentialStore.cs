namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public interface ICentralApiCredentialStore
    {
        Task<CentralApiConnectionSettings?> GetAsync();

        Task SaveAsync(CentralApiConnectionSettings settings);

        Task ClearAsync();
    }
}