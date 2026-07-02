using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class BarsPage : ContentPage
    {
        private readonly BarsViewModel _viewModel;

        public BarsPage(BarsViewModel viewModel)
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