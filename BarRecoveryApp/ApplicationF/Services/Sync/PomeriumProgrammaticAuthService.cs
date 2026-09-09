namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    // Implementa el flujo oficial de "Programmatic Access" de Pomerium:
    // https://www.pomerium.com/docs/internals/programmatic-access
    //
    // Flujo: la app pide una URL de login firmada -> abre el navegador del
    // sistema -> el usuario se autentica contra Entra ID -> Pomerium
    // redirige de vuelta a la app con un "pomerium_jwt" -> se guarda en
    // SecureStorage.
    //
    // El token persiste entre reinicios de la app (por eso SecureStorage y
    // no solo memoria) — el usuario vuelve a loguearse recién cuando la
    // sesión de Pomerium expira, aproximadamente una vez al mes.
    public class PomeriumProgrammaticAuthService : IPomeriumProgrammaticAuthService
    {
        // Debe coincidir exactamente con el esquema registrado en
        // Platforms/Android/WebAuthenticationCallbackActivity.cs
        private const string CallbackScheme = "barrecoveryapp";
        private const string CallbackHost = "pomerium-callback";

        private const string TokenStorageKey = "pomerium_programmatic_token";

        private readonly HttpClient _httpClient;
        private readonly ISyncCredentialStore _credentialStore;

        private readonly SemaphoreSlim _signInGate = new(1, 1);

        public PomeriumProgrammaticAuthService(
            HttpClient httpClient,
            ISyncCredentialStore credentialStore)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));
        }

        public async Task<string?> GetStoredTokenAsync()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync(TokenStorageKey);

                return string.IsNullOrWhiteSpace(token) ? null : token;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<PomeriumSignInResult> SignInAsync()
        {
            if (!await _signInGate.WaitAsync(0))
                return PomeriumSignInResult.Fail("Ya hay un inicio de sesión en curso.");

            try
            {
                var settings = await _credentialStore.GetAsync();

                if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                    return PomeriumSignInResult.Fail(
                        "Debe configurar la URL base antes de iniciar sesión.");

                var callbackUri = $"{CallbackScheme}://{CallbackHost}";

                // Paso 1: pedirle a Pomerium la URL de login firmada.
                // La respuesta es texto plano (la URL en sí), no JSON.
                var loginUrlEndpoint =
                    $"{settings.BaseUrl.TrimEnd('/')}/.pomerium/api/v1/login" +
                    $"?pomerium_redirect_uri={Uri.EscapeDataString(callbackUri)}";

                string signedLoginUrl;

                try
                {
                    signedLoginUrl = (await _httpClient.GetStringAsync(loginUrlEndpoint)).Trim();
                }
                catch (HttpRequestException ex)
                {
                    return PomeriumSignInResult.Fail(
                        $"No fue posible contactar el servidor: {ex.Message}");
                }
                catch (TaskCanceledException)
                {
                    return PomeriumSignInResult.Fail(
                        "Se agotó el tiempo de espera al contactar el servidor.");
                }

                if (string.IsNullOrWhiteSpace(signedLoginUrl))
                    return PomeriumSignInResult.Fail(
                        "El servidor no devolvió una URL de inicio de sesión válida.");

                // Pasos 2-7: el navegador del sistema muestra el login de
                // Entra ID; al terminar, Pomerium redirige a
                // "barrecoveryapp://pomerium-callback?pomerium_jwt=..."
                WebAuthenticatorResult result;

                try
                {
                    result = await WebAuthenticator.Default.AuthenticateAsync(
                        new WebAuthenticatorOptions
                        {
                            Url = new Uri(signedLoginUrl),
                            CallbackUrl = new Uri(callbackUri)
                        });
                }
                catch (TaskCanceledException)
                {
                    return PomeriumSignInResult.Fail(
                        "Inicio de sesión cancelado.");
                }
                catch (Exception ex)
                {
                    return PomeriumSignInResult.Fail(
                        $"Error durante el inicio de sesión: {ex.Message}");
                }

                if (!result.Properties.TryGetValue("pomerium_jwt", out var jwt) ||
                    string.IsNullOrWhiteSpace(jwt))
                {
                    return PomeriumSignInResult.Fail(
                        "El servidor no devolvió un token de sesión.");
                }

                await SecureStorage.Default.SetAsync(TokenStorageKey, jwt);

                return PomeriumSignInResult.Ok();
            }
            finally
            {
                _signInGate.Release();
            }
        }

        public Task ClearStoredTokenAsync()
        {
            SecureStorage.Default.Remove(TokenStorageKey);

            return Task.CompletedTask;
        }
    }
}