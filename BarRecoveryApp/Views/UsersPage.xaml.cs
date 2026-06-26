using BarRecoveryApp.ViewModels;

namespace BarRecoveryApp.Views
{
    public partial class UsersPage : ContentPage
    {
        private readonly UsersViewModel _viewModel;

        public UsersPage(UsersViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel
                ?? throw new ArgumentNullException(nameof(viewModel));

            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel.Users.Count == 0)
            {
                _viewModel.LoadCommand.Execute(null);
            }
        }
    }
}