using System.Collections.ObjectModel;
using System.Diagnostics;
using BarRecoveryApp.ApplicationF.Services.Users;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ViewModels
{
    public class UsersViewModel : BaseViewModel
    {
        private readonly IUserManagementService _userManagementService;

        private User? _selectedUser;
        private Role? _selectedRole;

        private string _username = string.Empty;
        private string _displayName = string.Empty;
        private string _initialPin = string.Empty;
        private string _resetPin = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;
        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public UsersViewModel(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService
                ?? throw new ArgumentNullException(nameof(userManagementService));

            Title = "Usuarios";

            Users = new ObservableCollection<User>();
            Roles = new ObservableCollection<Role>();

            LoadCommand = new RelayCommand(LoadAsync);
            CreateUserCommand = new RelayCommand(CreateUserAsync, CanCreateUser);
            ToggleActiveCommand = new RelayCommand(ToggleActiveAsync, CanSelectUser);
            ResetPinCommand = new RelayCommand(ResetPinAsync, CanResetPin);
            ClearFormCommand = new RelayCommand(ClearFormAsync);
        }

        public ObservableCollection<User> Users { get; }

        public ObservableCollection<Role> Roles { get; }

        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public Role? SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (SetProperty(ref _displayName, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string InitialPin
        {
            get => _initialPin;
            set
            {
                if (SetProperty(ref _initialPin, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string ResetPin
        {
            get => _resetPin;
            set
            {
                if (SetProperty(ref _resetPin, value))
                {
                    ClearMessages();
                    RefreshCommands();
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

        public string SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        public bool HasSuccess
        {
            get => _hasSuccess;
            set => SetProperty(ref _hasSuccess, value);
        }

        public RelayCommand LoadCommand { get; }

        public RelayCommand CreateUserCommand { get; }

        public RelayCommand ToggleActiveCommand { get; }

        public RelayCommand ResetPinCommand { get; }

        public RelayCommand ClearFormCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                Users.Clear();
                Roles.Clear();

                var roles = await _userManagementService.GetAvailableRolesForCurrentUserAsync();

                foreach (var role in roles)
                    Roles.Add(role);

                var users = await _userManagementService.GetUsersAsync();

                foreach (var user in users)
                    Users.Add(user);
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

        private async Task CreateUserAsync()
        {
            if (IsBusy)
                return;

            if (!CanCreateUser())
            {
                ShowError("Complete los datos requeridos correctamente.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var created = await _userManagementService.CreateUserAsync(
                    Username,
                    DisplayName,
                    SelectedRole!.Id,
                    InitialPin);

                if (!created)
                {
                    ShowError("No fue posible crear el usuario. Verifique si el usuario ya existe o si no tiene permisos.");
                    return;
                }

                ShowSuccess("Usuario creado correctamente.");

                Username = string.Empty;
                DisplayName = string.Empty;
                InitialPin = string.Empty;
                SelectedRole = null;

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error creando usuario: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedUser is null)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                var newState = !SelectedUser.IsActive;

                var changed = await _userManagementService.SetUserActiveStateAsync(
                    SelectedUser.Id,
                    newState);

                if (!changed)
                {
                    ShowError("No fue posible cambiar el estado del usuario.");
                    return;
                }

                ShowSuccess(newState
                    ? "Usuario activado correctamente."
                    : "Usuario desactivado correctamente.");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error cambiando estado: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ResetPinAsync()
        {
            if (SelectedUser is null)
            {
                ShowError("Debe seleccionar un usuario.");
                return;
            }

            if (!IsValidPin(ResetPin))
            {
                ShowError("El nuevo PIN debe tener entre 4 y 6 dígitos numéricos.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var reset = await _userManagementService.ResetUserPinAsync(
                    SelectedUser.Id,
                    ResetPin);

                if (!reset)
                {
                    ShowError("No fue posible resetear el PIN.");
                    return;
                }

                ResetPin = string.Empty;

                ShowSuccess("PIN reseteado correctamente. El usuario deberá cambiarlo al ingresar.");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error reseteando PIN: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task ClearFormAsync()
        {
            Username = string.Empty;
            DisplayName = string.Empty;
            InitialPin = string.Empty;
            ResetPin = string.Empty;
            SelectedRole = null;
            SelectedUser = null;
            ClearMessages();

            return Task.CompletedTask;
        }

        private bool CanCreateUser()
        {
            return !IsBusy
                   && !string.IsNullOrWhiteSpace(Username)
                   && !string.IsNullOrWhiteSpace(DisplayName)
                   && SelectedRole is not null
                   && IsValidPin(InitialPin);
        }

        private bool CanSelectUser()
        {
            var result = !IsBusy && SelectedUser is not null;
            return result;
        }

        private bool CanResetPin()
        {
            return !IsBusy
                   && SelectedUser is not null
                   && IsValidPin(ResetPin);
        }

        private static bool IsValidPin(string pin)
        {
            return !string.IsNullOrWhiteSpace(pin)
                   && pin.Length >= 4
                   && pin.Length <= 6
                   && pin.All(char.IsDigit);
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;

            SuccessMessage = string.Empty;
            HasSuccess = false;
        }

        private void ShowSuccess(string message)
        {
            SuccessMessage = message;
            HasSuccess = true;

            ErrorMessage = string.Empty;
            HasError = false;
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            HasError = false;

            SuccessMessage = string.Empty;
            HasSuccess = false;
        }

        private void RefreshCommands()
        {
            CreateUserCommand.RaiseCanExecuteChanged();
            ToggleActiveCommand.RaiseCanExecuteChanged();
            ResetPinCommand.RaiseCanExecuteChanged();
            ClearFormCommand.RaiseCanExecuteChanged();
            LoadCommand.RaiseCanExecuteChanged();
        }
    }
}