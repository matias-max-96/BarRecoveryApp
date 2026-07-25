using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class RecoveryWorkReportSyncApiClient : IRecoveryWorkReportSyncApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly ICentralApiAuthClient _authClient;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public RecoveryWorkReportSyncApiClient(
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

        public async Task<List<RecoveryWorkReportSyncDto>?> GetChangedSinceAsync(DateTime? since)
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return null;

            var token = await _authClient.GetValidTokenAsync();

            if (token is null)
                return null;

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/sync/recovery-work-reports";

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

                return JsonSerializer.Deserialize<List<RecoveryWorkReportSyncDto>>(body, JsonOptions)
                    ?? new List<RecoveryWorkReportSyncDto>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<RecoveryWorkReportPushResult> PushAsync(RecoveryWorkReportSyncDto dto)
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return RecoveryWorkReportPushResult.NotAuthenticated;

            var token = await _authClient.GetValidTokenAsync();

            if (token is null)
                return RecoveryWorkReportPushResult.NotAuthenticated;

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/sync/recovery-work-reports";

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
                    return RecoveryWorkReportPushResult.NotAuthenticated;
                }

                return response.IsSuccessStatusCode
                    ? RecoveryWorkReportPushResult.Success
                    : RecoveryWorkReportPushResult.NetworkError;
            }
            catch (Exception)
            {
                return RecoveryWorkReportPushResult.NetworkError;
            }
        }
    }
}