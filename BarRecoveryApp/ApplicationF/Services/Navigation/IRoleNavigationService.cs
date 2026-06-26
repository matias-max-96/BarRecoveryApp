namespace BarRecoveryApp.ApplicationF.Services.Navigation
{
    public interface IRoleNavigationService
    {
        Task NavigateToHomeAsync();

        Task LogoutAndGoToLoginAsync();
    }
}
