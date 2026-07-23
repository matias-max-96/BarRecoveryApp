using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class CentralApiAuthClient : ICentralApiAuthClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICentralApiCredentialStore _credentialStore;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private string? _cachedToken;
        private DateTime? _cachedTokenExpiresAtUtc;

        private readonly SemaphoreSlim _loginGate = new(1, 1);

        public CentralApiAuthClient(
            HttpClient httpClient,
            ICentralApiCredentialStore credentialStore)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));
        }

        public async Task<string?> GetValidTokenAsync()
        {
            if (_cachedToken is not null &&
                _cachedTokenExpiresAtUtc is not null &&
                _cachedTokenExpiresAtUtc.Value > DateTime.UtcNow.AddMinutes(2))
            {
                return _cachedToken;
            }

            await _loginGate.WaitAsync();

            try
            {

                if (_cachedToken is not null &&
                    _cachedTokenExpiresAtUtc is not null &&
                    _cachedTokenExpiresAtUtc.Value > DateTime.UtcNow.AddMinutes(2))
                {
                    return _cachedToken;
                }

                var settings = await _credentialStore.GetAsync();

                if (settings is null ||
                    string.IsNullOrWhiteSpace(settings.BaseUrl) ||
                    string.IsNullOrWhiteSpace(settings.Username))
                {
                    return null;
                }

                var loginPayload = JsonSerializer.Serialize(new
                {
                    username = settings.Username,
                    password = settings.Password
                });

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{settings.BaseUrl.TrimEnd('/')}/api/auth/login");

                request.Content = new StringContent(loginPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync();
                var loginResult = JsonSerializer.Deserialize<LoginResponse>(body, JsonOptions);

                if (loginResult is null || string.IsNullOrWhiteSpace(loginResult.AccessToken))
                {
                    return null;
                }

                _cachedToken = loginResult.AccessToken;
                _cachedTokenExpiresAtUtc = loginResult.ExpiresAtUtc;

                return _cachedToken;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                _loginGate.Release();
            }
        }

        public void InvalidateCachedToken()
        {
            _cachedToken = null;
            _cachedTokenExpiresAtUtc = null;
        }

        private class LoginResponse
        {
            public string AccessToken { get; set; } = string.Empty;

            public DateTime ExpiresAtUtc { get; set; }
        }
    }
}