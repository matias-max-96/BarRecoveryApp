using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    // Implementa el flujo oficial de "Programmatic Access" de Pomerium:
    // https://www.pomerium.com/docs/internals/programmatic-access
    //
    // IMPORTANTE — por qué esto usa un servidor HTTP local en vez de un
    // esquema de app personalizado ("barrecoveryapp://..."): Pomerium tuvo
    // una vulnerabilidad real (CVE-2021-29651, "JWT leak via Open Redirect
    // in Programmatic access") relacionada con redirects arbitrarios en
    // este mismo mecanismo. El fix agregó "programmatic_redirect_domain_
    // whitelist", y en la práctica Pomerium exige que el redirect_uri sea
    // http/https — un esquema de app personalizado se rechaza de plano con
    // 400 "invalid redirect uri", sin importar el whitelist. La app arma
    // entonces un mini servidor HTTP en el propio dispositivo
    // (http://127.0.0.1:{puerto}), que "localhost" ya acepta por defecto.
    //
    // Flujo: la app levanta el servidor local -> pide una URL de login
    // firmada -> abre el navegador del sistema -> el usuario se autentica
    // contra Entra ID -> Pomerium redirige a http://127.0.0.1:{puerto}/
    // con el "pomerium_jwt" -> el servidor local lo captura y se apaga.
    public class PomeriumProgrammaticAuthService : IPomeriumProgrammaticAuthService
    {
        private const string TokenStorageKey = "pomerium_programmatic_token";

        private static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(5);

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
                // En Android el keystore puede invalidar lo guardado (ej.
                // cambio de PIN del dispositivo) — se trata como "no hay
                // token", no como error fatal.
                return null;
            }
        }

        public async Task<PomeriumSignInResult> SignInAsync()
        {
            // Evita que dos toques rápidos abran dos navegadores/servidores.
            if (!await _signInGate.WaitAsync(0))
                return PomeriumSignInResult.Fail("Ya hay un inicio de sesión en curso.");

            HttpListener? listener = null;

            try
            {
                var settings = await _credentialStore.GetAsync();

                if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
                    return PomeriumSignInResult.Fail(
                        "Debe configurar la URL base antes de iniciar sesión.");

                var port = GetAvailableLoopbackPort();
                var callbackUri = $"http://127.0.0.1:{port}/";

                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add(callbackUri);
                    listener.Start();
                }
                catch (Exception ex)
                {
                    return PomeriumSignInResult.Fail(
                        $"No fue posible iniciar el servidor local para recibir el login: {ex.Message}");
                }

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

                // Paso 2: abrir el navegador del sistema — a diferencia de
                // WebAuthenticator, acá no esperamos que el navegador
                // "vuelva" a la app; el que captura la respuesta es el
                // servidor local de abajo.
                try
                {
                    await Browser.Default.OpenAsync(
                        new Uri(signedLoginUrl),
                        BrowserLaunchMode.SystemPreferred);
                }
                catch (Exception ex)
                {
                    return PomeriumSignInResult.Fail(
                        $"No fue posible abrir el navegador: {ex.Message}");
                }

                // Paso 3: esperar a que Pomerium redirija de vuelta al
                // servidor local con el token, con un límite de tiempo por
                // si el usuario abandona el login a mitad de camino.
                var contextTask = listener.GetContextAsync();
                var timeoutTask = Task.Delay(LoginTimeout);

                var completed = await Task.WhenAny(contextTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    return PomeriumSignInResult.Fail(
                        "Se agotó el tiempo de espera del inicio de sesión.");
                }

                var context = await contextTask;
                var jwt = context.Request.QueryString["pomerium_jwt"];

                await RespondToBrowserAsync(context, success: !string.IsNullOrWhiteSpace(jwt));

                if (string.IsNullOrWhiteSpace(jwt))
                {
                    return PomeriumSignInResult.Fail(
                        "El servidor no devolvió un token de sesión.");
                }

                await SecureStorage.Default.SetAsync(TokenStorageKey, jwt);

                return PomeriumSignInResult.Ok();
            }
            catch (Exception ex)
            {
                return PomeriumSignInResult.Fail($"Error durante el inicio de sesión: {ex.Message}");
            }
            finally
            {
                try
                {
                    listener?.Stop();
                    listener?.Close();
                }
                catch (Exception)
                {
                    // No hay nada más que hacer si el listener ya se cerró solo.
                }

                _signInGate.Release();
            }
        }

        public Task ClearStoredTokenAsync()
        {
            SecureStorage.Default.Remove(TokenStorageKey);

            return Task.CompletedTask;
        }

        // Le muestra al usuario una página simple de confirmación en el
        // navegador antes de que cierre esa pestaña y vuelva a la app.
        private static async Task RespondToBrowserAsync(HttpListenerContext context, bool success)
        {
            try
            {
                var message = success
                    ? "Inicio de sesión completado. Puede cerrar esta ventana y volver a la aplicación."
                    : "No fue posible completar el inicio de sesión. Puede cerrar esta ventana.";

                var html = $"<html><body style=\"font-family:sans-serif;text-align:center;padding-top:40px;\">" +
                           $"<h3>{message}</h3></body></html>";

                var buffer = Encoding.UTF8.GetBytes(html);

                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;

                await context.Response.OutputStream.WriteAsync(buffer);
                context.Response.OutputStream.Close();
            }
            catch (Exception)
            {
                // El usuario ya tiene lo que necesita (el token ya se leyó
                // de la query string); que falle la respuesta visual al
                // navegador no debe hacer fallar el login.
            }
        }

        // Encuentra un puerto loopback libre delegando en el sistema
        // operativo (bind a puerto 0), en vez de asumir uno fijo que podría
        // estar ocupado.
        private static int GetAvailableLoopbackPort()
        {
            var socket = new TcpListener(IPAddress.Loopback, 0);

            try
            {
                socket.Start();
                return ((IPEndPoint)socket.LocalEndpoint).Port;
            }
            finally
            {
                socket.Stop();
            }
        }
    }
}