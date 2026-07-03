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

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = _serviceProvider.GetRequiredService<AppShell>();
                });
            }
            catch (Exception ex)
            {
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
    }
}