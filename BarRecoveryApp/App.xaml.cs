using Microsoft.Extensions.DependencyInjection;
using BarRecoveryApp.Infrastructure.Persistence;
using BarRecoveryApp.Infrastructure.Persistence.Seed;

namespace BarRecoveryApp
{
    public partial class App : Application
    {
        public App(IDatabaseService databaseService,
            IDatabaseSeeder databaseSeeder)
        {
            InitializeComponent();

            _ = InitializeDatabaseAsync(databaseService, databaseSeeder);
        }
        private static async Task InitializeDatabaseAsync(
            IDatabaseService databaseService,
            IDatabaseSeeder databaseSeeder)
        {
            await databaseService.InitAsync();
            await databaseSeeder.SeedAsync();
            System.Diagnostics.Debug.WriteLine("Base de datos inicializada y seed ejecutado correctamente.");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}