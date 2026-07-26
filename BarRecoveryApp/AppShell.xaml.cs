using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Views;

namespace BarRecoveryApp
{
    public partial class AppShell : Shell
    {
        private readonly IServiceProvider _serviceProvider;

        public AppShell(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider
                ?? throw new ArgumentNullException(nameof(serviceProvider));

            Items.Add(new ShellContent
            {
                Title = "Login",
                Route = nameof(LoginPage),
                ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<LoginPage>())
            });

            Items.Add(new ShellContent
            {
                Title = "Administración",
                Route = nameof(AdminHomePage),
                ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<AdminHomePage>())
            });

            Items.Add(new ShellContent
            {
                Title = "Control Calidad",
                Route = nameof(QualityHomePage),
                ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<QualityHomePage>())
            });

            Items.Add(new ShellContent
            {
                Title = "Recuperación",
                Route = nameof(RecoveryHomePage),
                ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<RecoveryHomePage>())
            });

            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(ChangePinPage), typeof(ChangePinPage));
            Routing.RegisterRoute(nameof(AdminHomePage), typeof(AdminHomePage));
            Routing.RegisterRoute(nameof(QualityHomePage), typeof(QualityHomePage));
            Routing.RegisterRoute(nameof(RecoveryHomePage), typeof(RecoveryHomePage));
            Routing.RegisterRoute(nameof(UsersPage), typeof(UsersPage));
            Routing.RegisterRoute(nameof(PlantsPage), typeof(PlantsPage));
            Routing.RegisterRoute(nameof(BarTypesPage), typeof(BarTypesPage));
            Routing.RegisterRoute(nameof(ActivitiesPage), typeof(ActivitiesPage));
            Routing.RegisterRoute(nameof(SuppliesPage), typeof(SuppliesPage));
            Routing.RegisterRoute(nameof(BarAttributeDefinitionsPage), typeof(BarAttributeDefinitionsPage));
            Routing.RegisterRoute(nameof(BarRecoveryPoliciesPage), typeof(BarRecoveryPoliciesPage));
            Routing.RegisterRoute(nameof(BarsPage), typeof(BarsPage));
            Routing.RegisterRoute(nameof(RecoveryWorkReportPage), typeof(RecoveryWorkReportPage));
            Routing.RegisterRoute(nameof(RecoveryWorkReportsHistoryPage), typeof(RecoveryWorkReportsHistoryPage));
            Routing.RegisterRoute(nameof(QualityInspectionPage), typeof(QualityInspectionPage));
            Routing.RegisterRoute(nameof(ShipmentPage), typeof(ShipmentPage));
            Routing.RegisterRoute(nameof(BarReturnPage), typeof(BarReturnPage));
            Routing.RegisterRoute(nameof(BarHistoryPage), typeof(BarHistoryPage));
            Routing.RegisterRoute(nameof(BarImportPage), typeof(BarImportPage));
            Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
            Routing.RegisterRoute(nameof(ReportExportPage), typeof(ReportExportPage));
            Routing.RegisterRoute(nameof(BarLookupPage), typeof(BarLookupPage));
            Routing.RegisterRoute(nameof(AuditExportPage), typeof(AuditExportPage));
            Routing.RegisterRoute(nameof(MissingWorkReportPage), typeof(MissingWorkReportPage));
            Routing.RegisterRoute(nameof(SyncSettingsPage), typeof(SyncSettingsPage));
            Routing.RegisterRoute(nameof(CentralApiSettingsPage), typeof(CentralApiSettingsPage));

            // Verificación oportunista post-login (Fase 4 del sync): si el
            // backend central confirma en segundo plano que el usuario fue
            // desactivado en otra tablet, la sesión se limpia igual aunque
            // esta pantalla no lo sepa — acá reaccionamos visualmente,
            // redirigiendo a Login con el motivo. El evento puede dispararse
            // desde un hilo de background, por eso el MainThread.
            var currentUserService = _serviceProvider.GetRequiredService<ICurrentUserService>();

            currentUserService.SessionForceClosed += (sender, reason) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Current!.DisplayAlert("Sesión finalizada", reason, "OK");
                    await Current!.GoToAsync($"//{nameof(LoginPage)}");
                });
            };
        }
    }
}