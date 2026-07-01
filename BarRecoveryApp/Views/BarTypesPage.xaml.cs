using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class BarTypesPage : ContentPage
    {
        private readonly BarTypesViewModel _viewModel;

        public BarTypesPage(BarTypesViewModel viewModel)
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