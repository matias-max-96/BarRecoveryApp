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
    }
}