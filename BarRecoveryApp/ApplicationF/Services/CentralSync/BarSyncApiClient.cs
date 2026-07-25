using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class BarSyncApiClient : IBarSyncApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly ICentralApiAuthClient _authClient;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BarSyncApiClient(
            HttpClient httpClient,
            ICentralApiCredentialStore credentialStore,
            ICentralApiAuthClient authClient)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));

            _authClient = authClient
                ?? throw new ArgumentNullException(nameof(authClient));
        }

        public async Task<List<BarSyncDto>?> GetChangedSinceAsync(DateTime? since)
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return null;

            var token = await _authClient.GetValidTokenAsync();

            if (token is null)
                return null;

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/sync/bars";

            if (since.HasValue)
            {
                url += $"?since={Uri.EscapeDataString(since.Value.ToString("O"))}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authClient.InvalidateCachedToken();
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<BarSyncDto>>(body, JsonOptions)
                    ?? new List<BarSyncDto>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<BarPushOutcome> PushAsync(BarSyncDto dto)
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return new BarPushOutcome { Result = BarPushResult.NotAuthenticated };

            var token = await _authClient.GetValidTokenAsync();

            if (token is null)
                return new BarPushOutcome { Result = BarPushResult.NotAuthenticated };

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/sync/bars";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(dto),
                Encoding.UTF8,
                "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authClient.InvalidateCachedToken();
                    return new BarPushOutcome { Result = BarPushResult.NotAuthenticated };
                }

                var body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var conflict = JsonSerializer.Deserialize<BarSyncConflictBody>(body, JsonOptions);

                    return new BarPushOutcome
                    {
                        Result = BarPushResult.Conflict,
                        ServerVersion = conflict?.ServerVersion
                    };
                }

                if (!response.IsSuccessStatusCode)
                    return new BarPushOutcome { Result = BarPushResult.NetworkError };

                return new BarPushOutcome { Result = BarPushResult.Success };
            }
            catch (Exception)
            {
                return new BarPushOutcome { Result = BarPushResult.NetworkError };
            }
        }

        private class BarSyncConflictBody
        {
            public BarSyncDto? ServerVersion { get; set; }
        }
    }
}