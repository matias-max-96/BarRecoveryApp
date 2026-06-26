using BarRecoveryApp.Infrastructure.Persistence;
using BarRecoveryApp.Infrastructure.Persistence.Seed;

namespace BarRecoveryApp
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IDatabaseService _databaseService;
        private readonly IDatabaseSeeder _databaseSeeder;
        private readonly IServiceProvider _serviceProvider;

        public App(
            IDatabaseService databaseService,
            IDatabaseSeeder databaseSeeder,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _databaseService = databaseService;
            _databaseSeeder = databaseSeeder;
            _serviceProvider = serviceProvider;

            MainPage = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 12,
                    Children =
                    {
                        new ActivityIndicator
                        {
                            IsRunning = true,
                            WidthRequest = 48,
                            HeightRequest = 48
                        },
                        new Label
                        {
                            Text = "Inicializando base de datos...",
                            HorizontalTextAlignment = TextAlignment.Center
                        }
                    }
                }
            };

            _ = InitializeAppAsync();
        }

        private async Task InitializeAppAsync()
        {
            try
            {
                await _databaseService.InitAsync();

                await _databaseSeeder.SeedAsync();

                await DebugSeedResultAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = _serviceProvider.GetRequiredService<AppShell>();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR INICIALIZANDO APP:");
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = new ContentPage
                    {
                        Content = new Label
                        {
                            Text = $"Error inicializando la aplicación: {ex.Message}",
                            TextColor = Colors.Red,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    };
                });
            }
        }

        private async Task DebugSeedResultAsync()
        {
            var db = await _databaseService.GetConnectionAsync();

            var roles = await db.Table<Models.Security.Role>().ToListAsync();
            var users = await db.Table<Models.Security.User>().ToListAsync();

            System.Diagnostics.Debug.WriteLine("===== DEBUG SEED =====");
            System.Diagnostics.Debug.WriteLine($"Roles en DB: {roles.Count}");
            System.Diagnostics.Debug.WriteLine($"Usuarios en DB: {users.Count}");

            foreach (var role in roles)
            {
                System.Diagnostics.Debug.WriteLine($"ROL: {role.Code} - {role.Name}");
            }

            foreach (var user in users)
            {
                System.Diagnostics.Debug.WriteLine($"USER: {user.Username} - {user.DisplayName}");
            }

            System.Diagnostics.Debug.WriteLine("===== FIN DEBUG SEED =====");
        }
    }
}