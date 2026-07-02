using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class RecoveryRecordPage : ContentPage
    {
        private readonly RecoveryRecordViewModel _viewModel;

        public RecoveryRecordPage(RecoveryRecordViewModel viewModel)
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