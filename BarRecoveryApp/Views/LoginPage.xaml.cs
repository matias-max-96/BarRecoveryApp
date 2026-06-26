using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginViewModel _viewModel;

        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));

            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (_viewModel.Users.Count == 0)
                {
                    _viewModel.LoadUsersCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR EN LoginPage.OnAppearing:");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}