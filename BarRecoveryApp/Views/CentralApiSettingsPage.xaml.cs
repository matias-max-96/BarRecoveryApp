using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class CentralApiSettingsPage : ContentPage
    {
        private readonly CentralApiSettingsViewModel _viewModel;

        public CentralApiSettingsPage(CentralApiSettingsViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));

            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.LoadCommand.Execute(null);
        }
    }
}