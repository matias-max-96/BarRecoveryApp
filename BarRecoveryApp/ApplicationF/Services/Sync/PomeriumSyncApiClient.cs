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
        private readonly IPomeriumProgrammaticAuthService _programmaticAuthService;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PomeriumSyncApiClient(
            HttpClient httpClient,
            ISyncCredentialStore credentialStore,
            IPomeriumProgrammaticAuthService programmaticAuthService)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));

            _programmaticAuthService = programmaticAuthService
                ?? throw new ArgumentNullException(nameof(programmaticAuthService));
        }

        // Resuelve qué credencial usar. Prioridad:
        //   1. Token del flujo programático (Authorization: Pomerium <token>)
        //      — validado contra un Pomerium real: con token válido pasa,
        //        con token inválido devuelve 401.
        //   2. Cookie manual (Cookie: _pomerium=<valor>) — mecanismo viejo,
        //      se mantiene como respaldo para pruebas contra mocks y para
        //      no romper instalaciones ya configuradas.
        // Devuelve null si no hay ninguna de las dos.
        private async Task<(string HeaderName, string HeaderValue)?> ResolveAuthHeaderAsync(
            SyncConnectionSettings settings)
        {
            var programmaticToken = await _programmaticAuthService.GetStoredTokenAsync();

            if (!string.IsNullOrWhiteSpace(programmaticToken))
                return ("Authorization", $"Pomerium {programmaticToken}");

            if (!string.IsNullOrWhiteSpace(settings.PomeriumCookie))
                return ("Cookie", $"_pomerium={settings.PomeriumCookie}");

            return null;
        }

        public async Task<SyncApiResult<List<CustomerDto>>> GetCustomersAsync()
        {
            var settings = await GetSettingsOrNullAsync();

            if (settings is null)
                return SyncApiResult<List<CustomerDto>>.Fail(
                    "No hay una sesión de Pomerium configurada.",
                    requiresReAuthentication: true);

            var authHeader = await ResolveAuthHeaderAsync(settings);

            if (authHeader is null)
                return SyncApiResult<List<CustomerDto>>.Fail(
                    "No hay una sesión de Pomerium configurada.",
                    requiresReAuthentication: true);

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/v1/customers/";

            using var request = BuildRequest(HttpMethod.Get, url, authHeader.Value);

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

            var authHeader = await ResolveAuthHeaderAsync(settings);

            if (authHeader is null)
                return SyncApiResult<string>.Fail(
                    "No hay una sesión de Pomerium configurada.",
                    requiresReAuthentication: true);

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/v1/customers/{customerId}/";

            using var request = BuildRequest(HttpMethod.Get, url, authHeader.Value);

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

            var authHeader = await ResolveAuthHeaderAsync(settings);

            if (authHeader is null)
                return SyncApiResult.Fail(
                    "No hay una sesión de Pomerium configurada.",
                    requiresReAuthentication: true);

            var url = $"{settings.BaseUrl.TrimEnd('/')}/api/v1/customers/{customerId}/";

            using var request = BuildRequest(HttpMethod.Post, url, authHeader.Value);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            var result = await SendAsync<object?>(request, _ => Task.FromResult<object?>(null));

            return result.Success
                ? SyncApiResult.Ok()
                : SyncApiResult.Fail(
                    result.ErrorMessage ?? "Error desconocido.",
                    result.StatusCode,
                    result.RequiresReAuthentication);
        }

        // Solo valida que exista la URL base. La credencial se resuelve
        // aparte, en ResolveAuthHeaderAsync, porque ahora puede venir de dos
        // orígenes distintos (token programático o cookie manual).
        private async Task<SyncConnectionSettings?> GetSettingsOrNullAsync()
        {
            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                return null;

            return settings;
        }

        private static HttpRequestMessage BuildRequest(
            HttpMethod method,
            string url,
            (string HeaderName, string HeaderValue) authHeader)
        {
            var request = new HttpRequestMessage(method, url);

            // La credencial nunca se loguea ni se incluye en mensajes de error.
            request.Headers.Add(authHeader.HeaderName, authHeader.HeaderValue);
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