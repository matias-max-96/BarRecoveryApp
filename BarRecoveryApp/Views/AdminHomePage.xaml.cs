using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Navigation;

namespace BarRecoveryApp.Views;

public partial class AdminHomePage : ContentPage
{
    private readonly IRoleNavigationService _roleNavigationService;
    private readonly ICurrentUserService _currentUserService;
    public AdminHomePage(
        IRoleNavigationService  roleNavigationService,
        ICurrentUserService currentUserService)
	{
		InitializeComponent();

		_roleNavigationService = roleNavigationService
			?? throw new ArgumentNullException(nameof(roleNavigationService));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));
	}
    protected override void OnAppearing()
    {
        base.OnAppearing();

        ConfigureRolVisibility();
    }

    private void ConfigureRolVisibility()
    {
        var roleCode = _currentUserService.CurrentSession?.RoleCode;

        AuditLogButton.IsVisible = roleCode == "SUPER_ADMIN";
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
    private async void OnPlantsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PlantsPage));
    }
    private async void OnBarTypesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BarTypesPage));
    }
    private async void OnActivitiesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ActivitiesPage));
    }
    private async void OnSuppliesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SuppliesPage));
    }
    private async void OnBarAttributeDefinitionsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BarAttributeDefinitionsPage));
    }
    private async void OnBarRecoveryPoliciesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BarRecoveryPoliciesPage));
    }
    private async void OnBarsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BarsPage));
    }
    private async void OnBarImportClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BarImportPage));
    }
    private async void OnDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DashboardPage));
    }
    private async void OnReportExportClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ReportExportPage));
    }
    private async void OnAuditLogClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AuditExportPage));
    }
}