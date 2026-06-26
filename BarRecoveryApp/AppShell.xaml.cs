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
                ContentTemplate = new DataTemplate(() =>
                    _serviceProvider.GetRequiredService<LoginPage>())
            });

            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        }
    }
}