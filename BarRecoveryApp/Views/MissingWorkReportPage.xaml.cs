using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class MissingWorkReportPage : ContentPage
    {
        private readonly MissingWorkReportViewModel _viewModel;

        public MissingWorkReportPage(
            MissingWorkReportViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));

            BindingContext = _viewModel;
        }
    }
}