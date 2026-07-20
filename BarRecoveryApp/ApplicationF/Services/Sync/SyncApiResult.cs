namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public class SyncApiResult
    {
        public bool Success { get; set; }

        public int? StatusCode { get; set; }

        public string? ErrorMessage { get; set; }

        // Sesión de Pomerium vencida o sin acceso (401/403) — distinto de un error
        // transitorio de red, así el motor de sync puede decidir no reintentar
        // automáticamente y en vez de eso avisar que hay que re-autenticar.
        public bool RequiresReAuthentication { get; set; }

        public static SyncApiResult Ok() =>
            new() { Success = true };

        public static SyncApiResult Fail(
            string message,
            int? statusCode = null,
            bool requiresReAuthentication = false) =>
            new()
            {
                Success = false,
                ErrorMessage = message,
                StatusCode = statusCode,
                RequiresReAuthentication = requiresReAuthentication
            };
    }

    public class SyncApiResult<T> : SyncApiResult
    {
        public T? Data { get; set; }

        public static SyncApiResult<T> Ok(T data) =>
            new() { Success = true, Data = data };

        public new static SyncApiResult<T> Fail(
            string message,
            int? statusCode = null,
            bool requiresReAuthentication = false) =>
            new()
            {
                Success = false,
                ErrorMessage = message,
                StatusCode = statusCode,
                RequiresReAuthentication = requiresReAuthentication
            };
    }
}