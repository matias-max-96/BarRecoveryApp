using Microsoft.Extensions.Logging;
using BarRecoveryApp.Infrastructure.Persistence;
using BarRecoveryApp.Infrastructure.Persistence.Seed;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.ApplicationF.Services.Authentication;

namespace BarRecoveryApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
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

            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IDatabaseSeeder,  DatabaseSeeder>();
            builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddSingleton<ICurrentUserService, CurrentUserService>();
            builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();

            return builder.Build();
        }
    }
}
