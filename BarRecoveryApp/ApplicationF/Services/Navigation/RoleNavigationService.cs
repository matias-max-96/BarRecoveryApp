using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Views;
namespace BarRecoveryApp.ApplicationF.Services.Navigation
{
    public class RoleNavigationService : IRoleNavigationService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuthenticationService _authenticationService;

        public RoleNavigationService(
            ICurrentUserService currentUserService,
            IAuthenticationService authenticationService)
        {
            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));

            _authenticationService = authenticationService
                ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        public async Task NavigateToHomeAsync()
        {
            var session = _currentUserService.CurrentSession;

            if(session is null || !session.IsAuthenticated)
            {
                await Shell.Current.GoToAsync($"//{nameof(AdminHomePage)}");
                return;
            }

            switch (session.RoleCode)
            {
                case "SUPER_ADMIN":
                case "ADMIN":
                    await Shell.Current.GoToAsync($"//{nameof(AdminHomePage)}");
                    break;

                case "QUALITY":
                    await Shell.Current.GoToAsync($"//{nameof(QualityHomePage)}");
                    break;

                case "OPERATOR":
                    await Shell.Current.GoToAsync($"//{nameof(RecoveryHomePage)}");
                    break;

                default:
                    await Shell.Current.DisplayAlertAsync(
                        "Rol no encontrado",
                        $"El rol {session.RoleCode} no tiene una pantalla asignada",
                        "Aceptar");
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                    break;
            }
        }
        public async Task LogoutAndGoToLoginAsync()
        {
            await _authenticationService.LogoutAsync();

            await Shell.Current.GoToAsync($"{nameof(LoginPage)}");
        }
    }
}
