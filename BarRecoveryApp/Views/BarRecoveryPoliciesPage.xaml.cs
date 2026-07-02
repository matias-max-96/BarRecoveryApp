using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class BarRecoveryPoliciesPage : ContentPage
    {
        private readonly BarRecoveryPoliciesViewModel _viewModel;

        public BarRecoveryPoliciesPage(
            BarRecoveryPoliciesViewModel viewModel)
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