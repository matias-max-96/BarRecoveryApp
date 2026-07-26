namespace BarRecoveryApp.ApplicationF.Services.Authentication
{
    public interface ICurrentUserService
    {
        CurrentUserSession? CurrentSession { get; }

        bool IsAuthenticated { get; }

        void SetSession(CurrentUserSession session);

        void ClearSession();

        void UpdateActivity();

        bool HasPermission(string permissionCode);

        // Se dispara cuando la sesión se cierra por verificación remota
        // (ej. el backend central confirmó que el usuario fue desactivado
        // en otra tablet) — distinto de un logout manual del usuario. La UI
        // puede suscribirse para redirigir a la pantalla de login con un
        // mensaje explicativo.
        event EventHandler<string>? SessionForceClosed;

        void ForceCloseSession(string reason);
    }
}