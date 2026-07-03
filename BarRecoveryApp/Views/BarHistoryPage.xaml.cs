using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class BarHistoryPage : ContentPage
    {
        private readonly BarHistoryViewModel _viewModel;

        public BarHistoryPage(BarHistoryViewModel viewModel)
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