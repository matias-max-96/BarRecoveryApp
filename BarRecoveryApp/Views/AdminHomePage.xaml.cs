using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.CentralSync;
using BarRecoveryApp.ApplicationF.Services.Navigation;
using BarRecoveryApp.ApplicationF.Services.Sync;

namespace BarRecoveryApp.Views;

public partial class AdminHomePage : ContentPage
{
    private readonly IRoleNavigationService _roleNavigationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISyncEngineService _syncEngineService;
    private readonly IPlantSyncEngine _plantSyncEngine;
    private readonly IRecoveryWorkReportSyncEngine _recoveryWorkReportSyncEngine;
    private readonly IQualityInspectionSyncEngine _qualityInspectionSyncEngine;
    private readonly IShipmentCentralSyncEngine _shipmentCentralSyncEngine;
    private readonly IBarReturnReceiptSyncEngine _barReturnReceiptSyncEngine;

    public AdminHomePage(
        IRoleNavigationService roleNavigationService,
        ICurrentUserService currentUserService,
        ISyncEngineService syncEngineService,
        IPlantSyncEngine plantSyncEngine,
        IRecoveryWorkReportSyncEngine recoveryWorkReportSyncEngine,
        IQualityInspectionSyncEngine qualityInspectionSyncEngine,
        IShipmentCentralSyncEngine shipmentCentralSyncEngine,
        IBarReturnReceiptSyncEngine barReturnReceiptSyncEngine)
    {
        InitializeComponent();

        _roleNavigationService = roleNavigationService
            ?? throw new ArgumentNullException(nameof(roleNavigationService));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _syncEngineService = syncEngineService
            ?? throw new ArgumentNullException(nameof(syncEngineService));

        _plantSyncEngine = plantSyncEngine
            ?? throw new ArgumentNullException(nameof(plantSyncEngine));

        _recoveryWorkReportSyncEngine = recoveryWorkReportSyncEngine
            ?? throw new ArgumentNullException(nameof(recoveryWorkReportSyncEngine));

        _qualityInspectionSyncEngine = qualityInspectionSyncEngine
            ?? throw new ArgumentNullException(nameof(qualityInspectionSyncEngine));

        _shipmentCentralSyncEngine = shipmentCentralSyncEngine
            ?? throw new ArgumentNullException(nameof(shipmentCentralSyncEngine));

        _barReturnReceiptSyncEngine = barReturnReceiptSyncEngine
            ?? throw new ArgumentNullException(nameof(barReturnReceiptSyncEngine));
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
    private async void OnMissingWorkReportsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MissingWorkReportPage));
    }

    private async void OnSyncSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SyncSettingsPage));
    }

    private async void OnCentralApiSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CentralApiSettingsPage));
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
            var plantSummary = await _plantSyncEngine.SyncAsync();
            var reportSummary = await _recoveryWorkReportSyncEngine.SyncAsync();
            var inspectionSummary = await _qualityInspectionSyncEngine.SyncAsync();
            var shipmentCentralSummary = await _shipmentCentralSyncEngine.SyncAsync();
            var barReturnSummary = await _barReturnReceiptSyncEngine.SyncAsync();

            var messageLines = new List<string>
            {
                $"Reportes (Pomerium) — Procesados: {summary.Processed} | Exitosos: {summary.Succeeded} | Fallidos: {summary.Failed}"
            };

            if (plantSummary.NotConfigured)
            {
                messageLines.Add("Plantas (backend central) — no hay conexión configurada.");
            }
            else
            {
                messageLines.Add(
                    $"Plantas (backend central) — Bajadas: {plantSummary.Pulled} | Subidas: {plantSummary.Pushed} | Conflictos resueltos: {plantSummary.PushConflicts}");
            }

            if (!reportSummary.NotConfigured)
            {
                messageLines.Add(
                    $"Registros de trabajo (backend central) — Bajados: {reportSummary.Pulled} | Subidos: {reportSummary.Pushed}");
            }

            if (!inspectionSummary.NotConfigured)
            {
                messageLines.Add(
                    $"Inspecciones de calidad (backend central) — Bajadas: {inspectionSummary.Pulled} | Subidas: {inspectionSummary.Pushed}");
            }

            if (!shipmentCentralSummary.NotConfigured)
            {
                messageLines.Add(
                    $"Envíos (backend central) — Bajados: {shipmentCentralSummary.Pulled} | Subidos: {shipmentCentralSummary.Pushed}");
            }

            if (!barReturnSummary.NotConfigured)
            {
                messageLines.Add(
                    $"Retornos de barras (backend central) — Bajados: {barReturnSummary.Pulled} | Subidos: {barReturnSummary.Pushed}");
            }

            if (summary.RequiresReAuthentication)
            {
                messageLines.Add("");
                messageLines.Add("La sesión con el servidor remoto de reportes no está configurada, o venció.");
            }

            await DisplayAlertAsync(
                "Sincronización",
                string.Join("\n", messageLines),
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