using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class UserSyncApiClient : IUserSyncApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly ICentralApiAuthClient _authClient;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public UserSyncApiClient(
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

        public async Task<List<UserSyncDto>?> GetChangedSinceAsync(DateTime? since)
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return null;

            var token = await _authClient.GetValidTokenAsync();

            if (token is null)
                return null;

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/sync/users";

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

                return JsonSerializer.Deserialize<List<UserSyncDto>>(body, JsonOptions)
                    ?? new List<UserSyncDto>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<UserPushOutcome> PushAsync(UserSyncDto dto)
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return new UserPushOutcome { Result = UserPushResult.NotAuthenticated };

            var token = await _authClient.GetValidTokenAsync();

            if (token is null)
                return new UserPushOutcome { Result = UserPushResult.NotAuthenticated };

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/sync/users";

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
                    return new UserPushOutcome { Result = UserPushResult.NotAuthenticated };
                }

                var body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var conflict = JsonSerializer.Deserialize<UserSyncConflictBody>(body, JsonOptions);

                    return new UserPushOutcome
                    {
                        Result = UserPushResult.Conflict,
                        ServerVersion = conflict?.ServerVersion
                    };
                }

                if (!response.IsSuccessStatusCode)
                    return new UserPushOutcome { Result = UserPushResult.NetworkError };

                return new UserPushOutcome { Result = UserPushResult.Success };
            }
            catch (Exception)
            {
                return new UserPushOutcome { Result = UserPushResult.NetworkError };
            }
        }

        public async Task<bool?> CheckUserActiveAsync(string userId)
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return null;

            var token = await _authClient.GetValidTokenAsync();

            if (token is null)
                return null;

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/sync/users/{userId}/active-status";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<UserActiveStatusDto>(body, JsonOptions);

                return result?.IsActive;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private class UserSyncConflictBody
        {
            public UserSyncDto? ServerVersion { get; set; }
        }
    }
}