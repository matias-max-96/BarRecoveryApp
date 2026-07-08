using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class BarLookupPage : ContentPage
    {
        private readonly BarLookupViewModel _viewModel;

        public BarLookupPage(BarLookupViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));

            BindingContext = _viewModel;
        }
    }
}