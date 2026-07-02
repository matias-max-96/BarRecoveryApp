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
    private async void OnRegisterRecoveryClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecoveryRecordPage));
    }
    private async void OnRecoveryWorkReportClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecoveryWorkReportPage));
    }
    private async void OnMyRecoveryReportsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecoveryWorkReportsHistoryPage));
    }
}