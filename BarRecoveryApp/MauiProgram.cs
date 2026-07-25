using BarRecoveryApp.ApplicationF.Services.Auditing;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.CentralSync;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using BarRecoveryApp.ApplicationF.Services.Navigation;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Sync;
using BarRecoveryApp.ApplicationF.Services.Users;
using BarRecoveryApp.Infrastructure.Persistence;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Infrastructure.Persistence.Seed;
using BarRecoveryApp.ViewModels;
using BarRecoveryApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SQLitePCL;

namespace BarRecoveryApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Batteries_V2.Init();
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            //db
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IDatabaseSeeder, DatabaseSeeder>();

            //Repositories
            builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));

            //Services
            builder.Services.AddSingleton<ICurrentUserService, CurrentUserService>();
            builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();
            builder.Services.AddTransient<IRoleNavigationService, RoleNavigationService>();
            builder.Services.AddTransient<IUserManagementService, UserManagementService>();
            builder.Services.AddTransient<IPlantService, PlantService>();
            builder.Services.AddTransient<IBarTypeService, BarTypeService>();
            builder.Services.AddTransient<IActivityService, ActivityService>();
            builder.Services.AddTransient<ISupplyService, SupplyService>();
            builder.Services.AddTransient<IBarAttributeDefinitionService, BarAttributeDefinitionService>();
            builder.Services.AddTransient<IBarRecoveryPolicyService, BarRecoveryPolicyService>();
            builder.Services.AddTransient<IBarService, BarService>();
            builder.Services.AddTransient<IRecoveryService, RecoveryService>();
            builder.Services.AddTransient<IRecoveryWorkReportService, RecoveryWorkReportService>();
            builder.Services.AddTransient<IQualityInspectionService, QualityInspectionService>();
            builder.Services.AddTransient<IShipmentService, ShipmentService>();
            builder.Services.AddTransient<IBarReturnService, BarReturnService>();
            builder.Services.AddTransient<IBarHistoryService, BarHistoryService>();
            builder.Services.AddTransient<IBarImportService, BarImportService>();
            builder.Services.AddTransient<IDashboardService, DashboardService>();
            builder.Services.AddTransient<IReportExportService, ReportExportService>();
            builder.Services.AddTransient<IBarLookupService, BarLookupService>();
            builder.Services.AddTransient<IAuditLogService, AuditLogService>();
            builder.Services.AddTransient<IShipmentTechnicalReportExportService, ShipmentTechnicalReportExportService>();
            builder.Services.AddTransient<IMissingWorkReportService, MissingWorkReportService>();

            //Sync
            builder.Services.AddSingleton<ISyncCredentialStore, SecureStorageSyncCredentialStore>();
            builder.Services.AddHttpClient<ISyncApiClient, PomeriumSyncApiClient>();
            builder.Services.AddTransient<ISyncEngineService, SyncEngineService>();
            builder.Services.AddSingleton<ISyncBackgroundRunner, SyncBackgroundRunner>();
            builder.Services.AddTransient<IShipmentSyncPayloadBuilder, ShipmentSyncPayloadBuilder>();

            //CentralSync (backend propio, compartido entre las 5 tablets)
            builder.Services.AddSingleton<ICentralApiCredentialStore, SecureStorageCentralApiCredentialStore>();
            builder.Services.AddHttpClient<ICentralApiAuthClient, CentralApiAuthClient>();
            builder.Services.AddHttpClient<IPlantSyncApiClient, PlantSyncApiClient>();
            builder.Services.AddTransient<IPlantSyncEngine, PlantSyncEngine>();
            builder.Services.AddHttpClient<IRecoveryWorkReportSyncApiClient, RecoveryWorkReportSyncApiClient>();
            builder.Services.AddTransient<IRecoveryWorkReportSyncEngine, RecoveryWorkReportSyncEngine>();
            builder.Services.AddHttpClient<IQualityInspectionSyncApiClient, QualityInspectionSyncApiClient>();
            builder.Services.AddTransient<IQualityInspectionSyncEngine, QualityInspectionSyncEngine>();
            builder.Services.AddHttpClient<IShipmentCentralSyncApiClient, ShipmentCentralSyncApiClient>();
            builder.Services.AddTransient<IShipmentCentralSyncEngine, ShipmentCentralSyncEngine>();

            //Login View Model
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();
            //Change PIN
            builder.Services.AddTransient<ChangePinViewModel>();
            builder.Services.AddTransient<ChangePinPage>();
            //Pages
            builder.Services.AddTransient<AdminHomePage>();
            builder.Services.AddTransient<QualityHomePage>();
            builder.Services.AddTransient<RecoveryHomePage>();
            //UserPage
            builder.Services.AddTransient<UsersViewModel>();
            builder.Services.AddTransient<UsersPage>();
            //PlantsPage
            builder.Services.AddTransient<PlantsViewModel>();
            builder.Services.AddTransient<PlantsPage>();
            //BarPage
            builder.Services.AddTransient<BarTypesViewModel>();
            builder.Services.AddTransient<BarTypesPage>();
            //ActivityPage
            builder.Services.AddTransient<ActivitiesViewModel>();
            builder.Services.AddTransient<ActivitiesPage>();
            //SuppliesPage
            builder.Services.AddTransient<SuppliesViewModel>();
            builder.Services.AddTransient<SuppliesPage>();
            //BarAttributePage
            builder.Services.AddTransient<BarAttributeDefinitionsViewModel>();
            builder.Services.AddTransient<BarAttributeDefinitionsPage>();
            //BarRecoveryPoliciesPage
            builder.Services.AddTransient<BarRecoveryPoliciesViewModel>();
            builder.Services.AddTransient<BarRecoveryPoliciesPage>();
            //BarsPage
            builder.Services.AddTransient<BarsViewModel>();
            builder.Services.AddTransient<BarsPage>();
            //RecoveveryRecordPage
            builder.Services.AddTransient<RecoveryWorkReportViewModel>();
            builder.Services.AddTransient<RecoveryWorkReportPage>();
            //RecoveryWorkReportsHistoryPage
            builder.Services.AddTransient<RecoveryWorkReportsHistoryViewModel>();
            builder.Services.AddTransient<RecoveryWorkReportsHistoryPage>();
            //QualityInsectionPage
            builder.Services.AddTransient<QualityInspectionViewModel>();
            builder.Services.AddTransient<QualityInspectionPage>();
            //ShipmentPage
            builder.Services.AddTransient<ShipmentViewModel>();
            builder.Services.AddTransient<ShipmentPage>();
            //BarReturnPage
            builder.Services.AddTransient<BarReturnViewModel>();
            builder.Services.AddTransient<BarReturnPage>();
            //BarHistoryPage
            builder.Services.AddTransient<BarHistoryViewModel>();
            builder.Services.AddTransient<BarHistoryPage>();
            //BarImportPage
            builder.Services.AddTransient<BarImportViewModel>();
            builder.Services.AddTransient<BarImportPage>();
            //DashBoardPage
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<DashboardPage>();
            //ReportExportExcel
            builder.Services.AddTransient<ReportExportViewModel>();
            builder.Services.AddTransient<ReportExportPage>();
            //BarLookUp For Operators Page
            builder.Services.AddTransient<BarLookupViewModel>();
            builder.Services.AddTransient<BarLookupPage>();
            //Audit
            builder.Services.AddTransient<AuditExportViewModel>();
            builder.Services.AddTransient<AuditExportPage>();
            //MissingWorkReportPage
            builder.Services.AddTransient<MissingWorkReportViewModel>();
            builder.Services.AddTransient<MissingWorkReportPage>();
            //SyncSettingsPage
            builder.Services.AddTransient<SyncSettingsViewModel>();
            builder.Services.AddTransient<SyncSettingsPage>();
            //CentralApiSettingsPage
            builder.Services.AddTransient<CentralApiSettingsViewModel>();
            builder.Services.AddTransient<CentralApiSettingsPage>();
            //Appshell
            builder.Services.AddSingleton<AppShell>();

            var app = builder.Build();

            // Arranca el loop de sync en background una sola vez, apenas la
            // app termina de armar el contenedor de dependencias.
            app.Services.GetRequiredService<ISyncBackgroundRunner>().Start();

            return app;
        }
    }
}