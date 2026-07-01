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
            //Appshell
            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}
