using BarRecoveryApp.ApplicationF.Services.Sync.Dtos;
using DocumentFormat.OpenXml.Presentation;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public class PomeriumSyncApiClient : ISyncApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ISyncCredentialStore _credentialStore;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PomeriumSyncApiClient(
            HttpClient httpClient,
            ISyncCredentialStore credentialStore)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));
        }

        public async Task<SyncApiResult<List<CustomerDto>>> GetCustomersAsync()
        {
            var settings = await GetSettingsOrNullAsync();

            if (settings is null)
                return SyncApiResult<List<CustomerDto>>.Fail(
                    "No hay una sesión de Pomerium configurada.",
                    requiresReAuthentication: true);

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/v1/customers/";

            using var request = BuildRequest(HttpMethod.Get, url, settings.PomeriumCookie);

            return await SendAsync<List<CustomerDto>>(request, async response =>
            {
                var body = await response.Content.ReadAsStringAsync();
                var parsed = JsonSerializer.Deserialize<CustomersResponseDto>(body, JsonOptions);

                return parsed?.Customers ?? new List<CustomerDto>();
            });
        }

        public async Task<SyncApiResult<string>> GetCustomerDataAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return SyncApiResult<string>.Fail("Debe indicar el CustomerId.");

            var settings = await GetSettingsOrNullAsync();

            if (settings is null)
                return SyncApiResult<string>.Fail(
                    "No hay una sesión de Pomerium configurada.",
                    requiresReAuthentication: true);

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/v1/customers/{customerId}/";

            using var request = BuildRequest(HttpMethod.Get, url, settings.PomeriumCookie);

            return await SendAsync<string>(request, async response =>
                await response.Content.ReadAsStringAsync());
        }

        public async Task<SyncApiResult> PostCustomerDataAsync(string customerId, string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return SyncApiResult.Fail("Debe indicar el CustomerId.");

            if (string.IsNullOrWhiteSpace(payloadJson))
                return SyncApiResult.Fail("El payload a enviar está vacío.");

            var settings = await GetSettingsOrNullAsync();

            if (settings is null)
                return SyncApiResult.Fail(
                    "No hay una sesión de Pomerium configurada.",
                    requiresReAuthentication: true);

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/v1/customers/{customerId}/";

            using var request = BuildRequest(HttpMethod.Post, url, settings.PomeriumCookie);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            var result = await SendAsync<object?>(request, _ => Task.FromResult<object?>(null));

            return result.Success
                ? SyncApiResult.Ok()
                : SyncApiResult.Fail(
                    result.ErrorMessage ?? "Error desconocido.",
                    result.StatusCode,
                    result.RequiresReAuthentication);
        }

        private async Task<SyncConnectionSettings?> GetSettingsOrNullAsync()
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.PomeriumCookie))
                return null;

            if (string.IsNullOrWhiteSpace(settings.BaseUrl))
                return null;

            return settings;
        }

        private static HttpRequestMessage BuildRequest(
            HttpMethod method,
            string url,
            string pomeriumCookie)
        {
            var request = new HttpRequestMessage(method, url);

            // Pomerium identifica la sesión por este cookie (JWT). No se loguea
            // ni se incluye en mensajes de error.
            request.Headers.Add("Cookie", $"_pomerium={pomeriumCookie}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return request;
        }

        private async Task<SyncApiResult<T>> SendAsync<T>(
            HttpRequestMessage request,
            Func<HttpResponseMessage, Task<T>> onSuccess)
        {
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (TaskCanceledException)
            {
                return SyncApiResult<T>.Fail("Se agotó el tiempo de espera al contactar el servidor.");
            }
            catch (HttpRequestException ex)
            {
                // Cubre además fallas de certificado TLS, típicas en dominios
                // internos (.local) con certificados propios no confiados por
                // el sistema — si aparece este error, hay que revisar el
                // certificado del servidor con quien administra Pomerium.
                return SyncApiResult<T>.Fail($"Error de conexión: {ex.Message}");
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return SyncApiResult<T>.Fail(
                        "La sesión de Pomerium expiró o no tiene acceso. Debe reautenticarse.",
                        (int)response.StatusCode,
                        requiresReAuthentication: true);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();

                    return SyncApiResult<T>.Fail(
                        $"El servidor respondió con error ({(int)response.StatusCode}): {errorBody}",
                        (int)response.StatusCode);
                }

                try
                {
                    var data = await onSuccess(response);
                    return SyncApiResult<T>.Ok(data!);
                }
                catch (JsonException ex)
                {
                    return SyncApiResult<T>.Fail($"No fue posible interpretar la respuesta del servidor: {ex.Message}");
                }
            }
        }
    }
}