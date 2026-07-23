using System.Text.Json;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class SecureStorageCentralApiCredentialStore : ICentralApiCredentialStore
    {
        private const string StorageKey = "central_api_connection_settings";

        public async Task<CentralApiConnectionSettings?> GetAsync()
        {
            string? json;

            try
            {
                json = await SecureStorage.Default.GetAsync(StorageKey);
            }
            catch (Exception)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<CentralApiConnectionSettings>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async Task SaveAsync(CentralApiConnectionSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var json = JsonSerializer.Serialize(settings);

            await SecureStorage.Default.SetAsync(StorageKey, json);
        }

        public Task ClearAsync()
        {
            SecureStorage.Default.Remove(StorageKey);

            return Task.CompletedTask;
        }
    }
}