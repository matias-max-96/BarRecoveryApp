using SQLitePCL;

using Microsoft.Extensions.Logging;
using BarRecoveryApp.Infrastructure.Persistence;
using BarRecoveryApp.Infrastructure.Persistence.Seed;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ViewModels;
using BarRecoveryApp.Views;

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

            //Auth - User
            builder.Services.AddSingleton<ICurrentUserService, CurrentUserService>();
            builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();

            //Login View Model
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();

            //Change PIN
            builder.Services.AddTransient<ChangePinViewModel>();
            builder.Services.AddTransient<ChangePinPage>();

            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}
