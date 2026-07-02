using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class RecoveryWorkReportPage : ContentPage
    {
        private readonly RecoveryWorkReportViewModel _viewModel;

        public RecoveryWorkReportPage(
            RecoveryWorkReportViewModel viewModel)
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