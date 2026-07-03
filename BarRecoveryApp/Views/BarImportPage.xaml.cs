using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class BarImportPage : ContentPage
    {
        private readonly BarImportViewModel _viewModel;

        public BarImportPage(BarImportViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));

            BindingContext = _viewModel;
        }
    }
}