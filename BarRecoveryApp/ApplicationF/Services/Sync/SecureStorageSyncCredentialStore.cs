using System.Text.Json;

namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public class SecureStorageSyncCredentialStore : ISyncCredentialStore
    {
        private const string StorageKey = "sync_connection_settings";

        public async Task<SyncConnectionSettings?> GetAsync()
        {
            string? json;

            try
            {
                json = await SecureStorage.Default.GetAsync(StorageKey);
            }
            catch (Exception)
            {
                // En algunos dispositivos Android el keystore puede invalidar
                // el valor guardado (ej: cambio de PIN del dispositivo).
                // Tratamos esto como "no hay credenciales", no como excepción fatal.
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<SyncConnectionSettings>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async Task SaveAsync(SyncConnectionSettings settings)
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