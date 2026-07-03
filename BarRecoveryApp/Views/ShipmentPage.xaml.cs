using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class ShipmentPage : ContentPage
    {
        private readonly ShipmentViewModel _viewModel;

        public ShipmentPage(ShipmentViewModel viewModel)
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

        private void OnBarCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is not CheckBox checkBox)
                return;

            if (checkBox.BindingContext is not ShipmentBarTargetDto bar)
                return;

            _viewModel.NotifyBarSelectionChanged(bar);
        }

        private void OnRemoveSelectedBarClicked(object sender, EventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.BindingContext is not ShipmentBarTargetDto bar)
                return;

            _viewModel.RemoveSelectedBar(bar);
        }
    }
}