namespace BarRecoveryApp.ApplicationF.Services.Authentication
{
    public class CurrentUserService : ICurrentUserService
    {
        public CurrentUserSession? CurrentSession { get; private set; }

        public bool IsAuthenticated =>
            CurrentSession is not null && CurrentSession.IsAuthenticated;

        public void SetSession(CurrentUserSession session)
        {
            CurrentSession = session;
        }

        public void ClearSession()
        {
            CurrentSession = null;
        }

        public void UpdateActivity()
        {
            if (CurrentSession is null)
                return;

            CurrentSession.LastActivityAt = DateTime.Now;
        }

        public bool HasPermission(string permissionCode)
        {
            if (CurrentSession is null)
                return false;

            return CurrentSession.HasPermission(permissionCode);
        }
    }
}