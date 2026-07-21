using BarRecoveryApp.ApplicationF.Services.Sync;

namespace BarRecoveryApp.ViewModels
{
    public class SyncSettingsViewModel : BaseViewModel
    {
        private readonly ISyncCredentialStore _credentialStore;

        private string _baseUrl = string.Empty;
        private string _customerId = string.Empty;
        private string _pomeriumCookie = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public SyncSettingsViewModel(ISyncCredentialStore credentialStore)
        {
            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));

            Title = "Configuración de sincronización";

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

        public string CustomerId
        {
            get => _customerId;
            set
            {
                if (SetProperty(ref _customerId, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        // Solo escritura desde la UI (no se precarga el valor guardado — ver
        // LoadAsync). Evita mostrar el JWT completo en pantalla otra vez.
        public string PomeriumCookie
        {
            get => _pomeriumCookie;
            set
            {
                if (SetProperty(ref _pomeriumCookie, value))
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
                    CustomerId = string.Empty;
                    PomeriumCookie = string.Empty;
                    return;
                }

                BaseUrl = settings.BaseUrl;
                CustomerId = settings.CustomerId;

                // Deliberadamente NO se precarga el cookie guardado: si el
                // usuario quiere reemplazarlo, lo escribe de nuevo. Así no
                // queda un JWT completo visible en pantalla por accidente.
                PomeriumCookie = string.Empty;

                SuccessMessage = string.IsNullOrWhiteSpace(settings.PomeriumCookie)
                    ? string.Empty
                    : "Hay una sesión guardada. Deje 'Cookie' en blanco para conservarla, o ingrese una nueva para reemplazarla.";
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
                ShowError("Debe ingresar la URL base y el CustomerId.");
                return;
            }

            try
            {
                IsBusy = true;
                RefreshCommands();
                ClearMessages();

                var existing = await _credentialStore.GetAsync();

                // Si el usuario dejó el cookie en blanco, se conserva el que
                // ya estaba guardado (para no pedir reingresarlo solo por
                // querer cambiar la URL o el CustomerId).
                var cookieToSave = string.IsNullOrWhiteSpace(PomeriumCookie)
                    ? existing?.PomeriumCookie ?? string.Empty
                    : PomeriumCookie.Trim();

                await _credentialStore.SaveAsync(new SyncConnectionSettings
                {
                    BaseUrl = BaseUrl.Trim(),
                    CustomerId = CustomerId.Trim(),
                    PomeriumCookie = cookieToSave
                });

                PomeriumCookie = string.Empty;

                ShowSuccess("Configuración de sincronización guardada.");
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
                CustomerId = string.Empty;
                PomeriumCookie = string.Empty;

                ShowSuccess("Configuración de sincronización eliminada.");
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
                   && !string.IsNullOrWhiteSpace(CustomerId);
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