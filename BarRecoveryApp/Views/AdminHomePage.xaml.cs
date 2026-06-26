using BarRecoveryApp.ApplicationF.Services.Navigation;

namespace BarRecoveryApp.Views;

public partial class AdminHomePage : ContentPage
{
    private readonly IRoleNavigationService _roleNavigationService;
    public AdminHomePage(IRoleNavigationService  roleNavigationService)
	{
		InitializeComponent();

		_roleNavigationService = roleNavigationService
			?? throw new ArgumentNullException(nameof(roleNavigationService));
	}
	private async void OnUsersClicked(Object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(UsersPage));
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