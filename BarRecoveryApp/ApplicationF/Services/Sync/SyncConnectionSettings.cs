namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    // No hereda de EntityBase a propósito: no vive en la BD local de SQLite,
    // sino en SecureStorage, separado de los datos de negocio de la app.
    public class SyncConnectionSettings
    {
        public string BaseUrl { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;

        // El valor del cookie _pomerium (JWT de sesión). Nunca debe loguearse
        // ni serializarse fuera de SecureStorage.
        public string PomeriumCookie { get; set; } = string.Empty;
    }
}