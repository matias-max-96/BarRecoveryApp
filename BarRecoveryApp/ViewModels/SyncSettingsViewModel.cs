using BarRecoveryApp.ApplicationF.Services.Sync;

namespace BarRecoveryApp.ViewModels
{
    public class SyncSettingsViewModel : BaseViewModel
    {
        private readonly ISyncCredentialStore _credentialStore;
        private readonly IPomeriumProgrammaticAuthService _programmaticAuthService;

        private bool _hasProgrammaticSession;

        private string _baseUrl = string.Empty;
        private string _customerId = string.Empty;
        private string _pomeriumCookie = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public SyncSettingsViewModel(
            ISyncCredentialStore credentialStore,
            IPomeriumProgrammaticAuthService programmaticAuthService)
        {
            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));

            _programmaticAuthService = programmaticAuthService
                ?? throw new ArgumentNullException(nameof(programmaticAuthService));

            Title = "Configuración de sincronización";

            LoadCommand = new RelayCommand(LoadAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ClearCommand = new RelayCommand(ClearAsync);
            SignInCommand = new RelayCommand(SignInAsync, CanSignIn);
            SignOutCommand = new RelayCommand(SignOutAsync, () => HasProgrammaticSession);
        }

        // true cuando ya hay un token del flujo programático guardado.
        public bool HasProgrammaticSession
        {
            get => _hasProgrammaticSession;
            private set
            {
                if (SetProperty(ref _hasProgrammaticSession, value))
                {
                    OnPropertyChanged(nameof(SessionStatusText));
                    RefreshCommands();
                }
            }
        }

        public string SessionStatusText => HasProgrammaticSession
            ? "Sesión iniciada. Se renovará solo cuando expire (aprox. una vez al mes)."
            : "Sin sesión iniciada. Debe iniciar sesión para poder sincronizar.";

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

        public RelayCommand SignInCommand { get; }

        public RelayCommand SignOutCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefreshCommands();
                ClearMessages();

                await RefreshSessionStateAsync();

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

                await SaveSettingsAsync();

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

        // Extraído de SaveAsync para poder reusarlo desde SignInAsync — así
        // "Iniciar sesión" guarda la URL base actual del formulario antes de
        // abrir el navegador, sin depender de que el usuario haya tocado
        // "Guardar" primero.
        private async Task SaveSettingsAsync()
        {
            var existing = await _credentialStore.GetAsync();

            // Si el usuario dejó el cookie en blanco, se conserva el que ya
            // estaba guardado (para no pedir reingresarlo solo por querer
            // cambiar la URL o el CustomerId).
            var cookieToSave = string.IsNullOrWhiteSpace(PomeriumCookie)
                ? existing?.PomeriumCookie ?? string.Empty
                : PomeriumCookie.Trim();

            await _credentialStore.SaveAsync(new SyncConnectionSettings
            {
                BaseUrl = BaseUrl.Trim(),
                CustomerId = CustomerId.Trim(),
                PomeriumCookie = cookieToSave
            });
        }

        private bool CanSignIn()
        {
            // Sin URL base no hay a dónde ir a autenticarse.
            return !IsBusy && !string.IsNullOrWhiteSpace(BaseUrl);
        }

        private async Task SignInAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefreshCommands();
                ClearMessages();

                // Guarda la URL base actual del formulario antes de intentar
                // el login — así no depende de que el usuario haya tocado
                // "Guardar" primero.
                await SaveSettingsAsync();

                // Abre el navegador del sistema. Debe dispararse solo desde
                // la UI (nunca desde el sync en background), por eso vive
                // acá y no dentro del motor de sincronización.
                var result = await _programmaticAuthService.SignInAsync();

                if (!result.Success)
                {
                    ShowError(result.ErrorMessage ?? "No fue posible iniciar sesión.");
                    return;
                }

                await RefreshSessionStateAsync();

                ShowSuccess("Sesión iniciada correctamente con Pomerium.");
            }
            catch (Exception ex)
            {
                ShowError($"Error al iniciar sesión: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task SignOutAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefreshCommands();
                ClearMessages();

                await _programmaticAuthService.ClearStoredTokenAsync();

                await RefreshSessionStateAsync();

                ShowSuccess("Sesión de Pomerium cerrada.");
            }
            catch (Exception ex)
            {
                ShowError($"Error al cerrar sesión: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task RefreshSessionStateAsync()
        {
            var token = await _programmaticAuthService.GetStoredTokenAsync();

            HasProgrammaticSession = !string.IsNullOrWhiteSpace(token);
        }

        private void RefreshCommands()
        {
            LoadCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            ClearCommand.RaiseCanExecuteChanged();
            SignInCommand.RaiseCanExecuteChanged();
            SignOutCommand.RaiseCanExecuteChanged();
        }
    }
}