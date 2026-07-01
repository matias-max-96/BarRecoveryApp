using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class ActivitiesPage : ContentPage
    {
        private readonly ActivitiesViewModel _viewModel;

        public ActivitiesPage(ActivitiesViewModel viewModel)
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