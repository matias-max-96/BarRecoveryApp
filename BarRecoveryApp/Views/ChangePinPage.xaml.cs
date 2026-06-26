using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class ChangePinPage : ContentPage
    {
        private readonly ChangePinViewModel _viewModel;

        public ChangePinPage(ChangePinViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));

            BindingContext = _viewModel;
        }
    }
}