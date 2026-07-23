using BarRecoveryApp.ApplicationF.Services.CentralSync;

namespace BarRecoveryApp.ViewModels
{
    public class CentralApiSettingsViewModel : BaseViewModel
    {
        private readonly ICentralApiCredentialStore _credentialStore;

        private string _baseUrl = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public CentralApiSettingsViewModel(ICentralApiCredentialStore credentialStore)
        {
            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));

            Title = "Configuración de backend central";

            LoadCommand = new RelayCommand(LoadAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public string BaseUrl
        {
            get => _baseUrl;
            set
            {
                if (SetProperty(ref _baseUrl, value))
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

        // Igual que con el cookie de Pomerium: no se precarga el valor
        // guardado (ver LoadAsync). Dejar en blanco = conservar el actual.
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
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

        public RelayCommand SaveCommand { get; }

        public RelayCommand ClearCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefreshCommands();
                ClearMessages();

                var settings = await _credentialStore.GetAsync();

                if (settings is null)
                {
                    BaseUrl = string.Empty;
                    Username = string.Empty;
                    Password = string.Empty;
                    return;
                }

                BaseUrl = settings.BaseUrl;
                Username = settings.Username;
                Password = string.Empty;

                SuccessMessage = string.IsNullOrWhiteSpace(settings.Password)
                    ? string.Empty
                    : "Hay una conexión guardada. Deje 'Contraseña' en blanco para conservarla, o ingrese una nueva para reemplazarla.";
                HasSuccess = !string.IsNullOrWhiteSpace(SuccessMessage);
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando configuración: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            if (!CanSave())
            {
                ShowError("Debe ingresar la URL base y el usuario.");
                return;
            }

            try
            {
                IsBusy = true;
                RefreshCommands();
                ClearMessages();

                var existing = await _credentialStore.GetAsync();

                var passwordToSave = string.IsNullOrWhiteSpace(Password)
                    ? existing?.Password ?? string.Empty
                    : Password;

                await _credentialStore.SaveAsync(new CentralApiConnectionSettings
                {
                    BaseUrl = BaseUrl.Trim(),
                    Username = Username.Trim(),
                    Password = passwordToSave
                });

                Password = string.Empty;

                ShowSuccess("Configuración de backend central guardada.");
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando configuración: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ClearAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefreshCommands();
                ClearMessages();

                await _credentialStore.ClearAsync();

                BaseUrl = string.Empty;
                Username = string.Empty;
                Password = string.Empty;

                ShowSuccess("Configuración de backend central eliminada.");
            }
            catch (Exception ex)
            {
                ShowError($"Error eliminando configuración: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private bool CanSave()
        {
            return !IsBusy
                   && !string.IsNullOrWhiteSpace(BaseUrl)
                   && !string.IsNullOrWhiteSpace(Username);
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
            LoadCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            ClearCommand.RaiseCanExecuteChanged();
        }
    }
}