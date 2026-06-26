using BarRecoveryApp.ApplicationF.Services.Navigation;

namespace BarRecoveryApp.Views;

public partial class RecoveryHomePage : ContentPage
{
    private readonly IRoleNavigationService _roleNavigationService;
    public RecoveryHomePage(IRoleNavigationService roleNavigationService)
	{
		InitializeComponent();
		_roleNavigationService = roleNavigationService
			?? throw new ArgumentNullException((nameof(roleNavigationService)));
	}
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Cerrar sesión",
            "¿Desea cerrar la sesión actual?",
            "Sí",
            "No");

        if (!confirm) return;

        await _roleNavigationService.LogoutAndGoToLoginAsync();
    }
}