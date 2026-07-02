using SQLitePCL;

using Microsoft.Extensions.Logging;
using BarRecoveryApp.Infrastructure.Persistence;
using BarRecoveryApp.Infrastructure.Persistence.Seed;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ViewModels;
using BarRecoveryApp.Views;
using BarRecoveryApp.ApplicationF.Services.Navigation;
using BarRecoveryApp.ApplicationF.Services.Users;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using BarRecoveryApp.ApplicationF.Services.Operations;

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
            builder.Services.AddSingleton<IDatabaseSeeder,  DatabaseSeeder>();

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
            //Appshell
            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}
