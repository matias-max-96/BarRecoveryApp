using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Navigation;
using BarRecoveryApp.ApplicationF.Services.Sync;

namespace BarRecoveryApp.Views;

public partial class AdminHomePage : ContentPage
{
    private readonly IRoleNavigationService _roleNavigationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISyncEngineService _syncEngineService;

    public AdminHomePage(
        IRoleNavigationService roleNavigationService,
        ICurrentUserService currentUserService,
        ISyncEngineService syncEngineService)
    {
        InitializeComponent();

        _roleNavigationService = roleNavigationService
            ?? throw new ArgumentNullException(nameof(roleNavigationService));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _syncEngineService = syncEngineService
            ?? throw new ArgumentNullException(nameof(syncEngineService));
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
        SyncButton.IsVisible = roleCode == "SUPER_ADMIN";
        SyncConfigButton.IsVisible = roleCode == "SUPER_ADMIN";
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
    private async void OnMissingWorkReportsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MissingWorkReportPage));
    }

    private async void OnSyncSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SyncSettingsPage));
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        // Deshabilitar de inmediato: evita disparar dos corridas del motor
        // de sync en paralelo si el usuario toca varias veces mientras espera.
        SyncButton.IsEnabled = false;
        SyncButton.Text = "Sincronizando...";

        try
        {
            var summary = await _syncEngineService.ProcessPendingAsync();

            if (summary.RequiresReAuthentication)
            {
                await DisplayAlertAsync(
                    "Sincronización",
                    $"Procesados: {summary.Processed} | Exitosos: {summary.Succeeded} | Fallidos: {summary.Failed}\n\n" +
                    "No hay una sesión válida con el servidor remoto configurada, o venció. Debe configurarla/reautenticarse antes de sincronizar.",
                    "OK");
                return;
            }

            if (summary.Processed == 0)
            {
                await DisplayAlertAsync(
                    "Sincronización",
                    "No hay elementos pendientes por sincronizar.",
                    "OK");
                return;
            }

            await DisplayAlertAsync(
                "Sincronización",
                $"Procesados: {summary.Processed} | Exitosos: {summary.Succeeded} | Fallidos: {summary.Failed}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Sincronización",
                $"Ocurrió un error inesperado al sincronizar: {ex.Message}",
                "OK");
        }
        finally
        {
            SyncButton.Text = "Sincronización";
            SyncButton.IsEnabled = true;
        }
    }
}