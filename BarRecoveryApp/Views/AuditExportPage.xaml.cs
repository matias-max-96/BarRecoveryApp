using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class AuditExportPage : ContentPage
    {
        private readonly AuditExportViewModel _viewModel;

        public AuditExportPage(AuditExportViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));

            BindingContext = _viewModel;
        }
    }
}