using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class ReportExportPage : ContentPage
    {
        private readonly ReportExportViewModel _viewModel;

        public ReportExportPage(ReportExportViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));

            BindingContext = _viewModel;
        }
    }
}