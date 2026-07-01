using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class SuppliesPage : ContentPage
    {
        private readonly SuppliesViewModel _viewModel;

        public SuppliesPage(SuppliesViewModel viewModel)
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