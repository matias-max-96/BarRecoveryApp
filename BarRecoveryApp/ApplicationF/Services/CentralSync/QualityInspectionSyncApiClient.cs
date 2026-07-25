using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class QualityInspectionSyncApiClient : IQualityInspectionSyncApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly ICentralApiAuthClient _authClient;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public QualityInspectionSyncApiClient(
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

        public async Task<List<QualityInspectionSyncDto>?> GetChangedSinceAsync(DateTime? since)
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return null;

            var token = await _authClient.GetValidTokenAsync();

            if (token is null)
                return null;

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/sync/quality-inspections";

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

                return JsonSerializer.Deserialize<List<QualityInspectionSyncDto>>(body, JsonOptions)
                    ?? new List<QualityInspectionSyncDto>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<QualityInspectionPushResult> PushAsync(QualityInspectionSyncDto dto)
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return QualityInspectionPushResult.NotAuthenticated;

            var token = await _authClient.GetValidTokenAsync();

            if (token is null)
                return QualityInspectionPushResult.NotAuthenticated;

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/sync/quality-inspections";

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
                    return QualityInspectionPushResult.NotAuthenticated;
                }

                return response.IsSuccessStatusCode
                    ? QualityInspectionPushResult.Success
                    : QualityInspectionPushResult.NetworkError;
            }
            catch (Exception)
            {
                return QualityInspectionPushResult.NetworkError;
            }
        }
    }
}