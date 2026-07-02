using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class QualityInspectionPage : ContentPage
    {
        private readonly QualityInspectionViewModel _viewModel;

        public QualityInspectionPage(
            QualityInspectionViewModel viewModel)
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