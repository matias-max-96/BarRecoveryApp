using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class SyncSettingsPage : ContentPage
    {
        private readonly SyncSettingsViewModel _viewModel;

        public SyncSettingsPage(SyncSettingsViewModel viewModel)
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