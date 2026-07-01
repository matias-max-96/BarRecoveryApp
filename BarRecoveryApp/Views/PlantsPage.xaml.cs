using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class PlantsPage : ContentPage
    {
        private readonly PlantsViewModel _viewModel;

        public PlantsPage(PlantsViewModel viewModel)
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