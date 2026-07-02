using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class RecoveryWorkReportsHistoryPage : ContentPage
    {
        private readonly RecoveryWorkReportsHistoryViewModel _viewModel;

        public RecoveryWorkReportsHistoryPage(
            RecoveryWorkReportsHistoryViewModel viewModel)
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