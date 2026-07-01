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
        }
    }
}