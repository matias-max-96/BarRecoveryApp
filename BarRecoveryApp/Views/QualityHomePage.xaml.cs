using BarRecoveryApp.ApplicationF.Services.Navigation;

namespace BarRecoveryApp.Views;

public partial class QualityHomePage : ContentPage
{
    private readonly IRoleNavigationService _roleNavigationService;
    public QualityHomePage(IRoleNavigationService roleNavigationService)
	{
		InitializeComponent();

        _roleNavigationService = roleNavigationService
            ?? throw new ArgumentNullException(nameof(roleNavigationService));
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
    private async void OnBarsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BarsPage));
    }
    private async void OnQualityInspectionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(QualityInspectionPage));
    }
}