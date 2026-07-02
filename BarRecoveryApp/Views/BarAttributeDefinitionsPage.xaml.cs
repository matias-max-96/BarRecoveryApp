using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class BarAttributeDefinitionsPage : ContentPage
    {
        private readonly BarAttributeDefinitionsViewModel _viewModel;

        public BarAttributeDefinitionsPage(
            BarAttributeDefinitionsViewModel viewModel)
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