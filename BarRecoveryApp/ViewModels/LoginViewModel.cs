using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Models.Security;
using BarRecoveryApp.Views;


namespace BarRecoveryApp.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authenticationService;

        private User? _selectedUser;
        private string _pin = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError;

        public LoginViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService
                ?? throw new ArgumentNullException(nameof(authenticationService));

            Title = "Ingreso";

            Users = new ObservableCollection<User>();

            LoadUsersCommand = new RelayCommand(LoadUsersAsync);
            LoginCommand = new RelayCommand(LoginAsync, CanLogin);
            ClearPinCommand = new RelayCommand(ClearPinAsync);
        }

        public ObservableCollection<User> Users { get; }

        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    RefreshCommands();
                    ClearError();
                }
            }
        }
        public string Pin
        {
            get => _pin;
            set
            {
                if (SetProperty(ref _pin, value))
                {
                    RefreshCommands();
                    ClearError();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public RelayCommand LoadUsersCommand { get; }

        public RelayCommand LoginCommand { get; }

        public RelayCommand ClearPinCommand { get; }


        private async Task LoadUsersAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                Users.Clear();

                var users = await _authenticationService.GetActiveUsersAsync();

                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando usuarios: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task LoginAsync()
        {
            if (IsBusy)
                return;

            if (SelectedUser is null)
            {
                ShowError("Debe seleccionar un usuario.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Pin))
            {
                ShowError("Debe ingresar el PIN.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearError();

                var result = await _authenticationService.LoginWithPinAsync(
                    SelectedUser.Id,
                    Pin);

                if (!result.Success)
                {
                    ShowError(result.Message);
                    Pin = string.Empty;
                    return;
                }

                Pin = string.Empty;

                if (result.MustChangePin)
                {
                    await Shell.Current.GoToAsync(nameof(ChangePinPage));
                    return;
                }

                await Shell.Current.DisplayAlertAsync(
                    "Ingreso correcto",
                    $"Bienvenido {result.User?.DisplayName}",
                    "Aceptar");
            }
            catch (Exception ex)
            {
                ShowError($"Error en login: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task ClearPinAsync()
        {
            Pin = string.Empty;
            ClearError();

            return Task.CompletedTask;
        }

        private bool CanLogin()
        {
            /*return !IsBusy &&
                   SelectedUser is not null &&
                   !string.IsNullOrWhiteSpace(Pin);*/
            return !IsBusy &&
                       SelectedUser is not null &&
                       Pin.Length >= 4 &&
                       Pin.Length <= 6 &&
                       Pin.All(char.IsDigit);
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        private void RefreshCommands()
        {
            LoginCommand.RaiseCanExecuteChanged();
            ClearPinCommand.RaiseCanExecuteChanged();
            LoadUsersCommand.RaiseCanExecuteChanged();
        }
    }
}
