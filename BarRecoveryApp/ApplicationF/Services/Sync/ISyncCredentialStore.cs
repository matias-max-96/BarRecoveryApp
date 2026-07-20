namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public interface ISyncCredentialStore
    {
        Task<SyncConnectionSettings?> GetAsync();

        Task SaveAsync(SyncConnectionSettings settings);

        Task ClearAsync();
    }
}