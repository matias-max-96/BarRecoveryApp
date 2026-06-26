using BarRecoveryApp.ApplicationF.Services.Authentication;

namespace BarRecoveryApp.ViewModels
{ 
    public class ChangePinViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ICurrentUserService _currentUserService;

        private string _currentPin = string.Empty;
        private string _newPin = string.Empty;
        private string _confirmNewPin = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError;

        public ChangePinViewModel(
            IAuthenticationService authenticationService,
            ICurrentUserService currentUserService) 
        {
            _authenticationService = authenticationService
                ?? throw new ArgumentNullException(nameof(authenticationService));

            _currentUserService = currentUserService 
                ?? throw new ArgumentNullException(nameof(currentUserService));

            Title = "Cambio de PIN";

            ChangePinCommand = new RelayCommand(ChangePinAsync, CanChangePin);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public string CurrentPin
        {
            get => _currentPin;
            set
            {
                if (SetProperty(ref _currentPin, value))
                {
                    ClearError();
                    RefreshCommands();
                }
            }
        }

        public string NewPin
        {
            get => _newPin;
            set
            {
                if(SetProperty(ref _newPin, value))
                {
                    ClearError();
                    RefreshCommands();
                }
            }
        }

        public string ConfirmNewPin
        {
            get => _confirmNewPin;
            set
            {
                if(SetProperty(ref _confirmNewPin, value))
                {
                    ClearError();
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

        public RelayCommand ChangePinCommand { get; }
        public RelayCommand ClearCommand { get; }

        private async Task ChangePinAsync()
        {
            if (IsBusy)
                return;

            if(_currentUserService.CurrentSession is null) 
            {
                ShowError("No existe una sesión activa");
                return;
            }
                
            if (!IsValidPin(CurrentPin))
            {
                ShowError("El pin actual debe tener entre 4 y 6 dígitos númericos");
                return;
            }

            if (!IsValidPin(NewPin))
            {
                ShowError("El nuevo PIn debe tener entre 4 y 6 dígitos numéricos.");
                return;
            }

            if (NewPin != ConfirmNewPin)
            {
                ShowError("La confirmación del nuevo PIN no coincide.");
                return;
            }

            if(CurrentPin == NewPin)
            {
                ShowError("El nuevo PIn debe ser diferente del PIN actual.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearError();

                var userId = _currentUserService.CurrentSession.UserId;

                var changed = await _authenticationService.ChangePinAsync(
                    userId,
                    CurrentPin,
                    NewPin);

                if (!changed)
                {
                    ShowError("No fue posible cambiar el PIN. Verifique el PIN actual");
                    return;
                }

                CurrentPin = string.Empty;
                NewPin = string.Empty;
                ConfirmNewPin = string.Empty;

                await Shell.Current.DisplayAlertAsync(
                    "El PIN actualizado",
                    "El PIN fue cambiado correctamente",
                    "Aceptar");

                /// MATIAS Y LA CONCHETUMARE QUE NO SE TE OLVIDE  CAMBIAR ESTA WEA PERRO QL
                /// 
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch(Exception ex)
            {
                ShowError($"Error cambiando PIN: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }
        private Task ClearAsync()
        {
            CurrentPin = string.Empty;
            NewPin = string.Empty;
            ConfirmNewPin = string.Empty;
            ClearError();

            return Task.CompletedTask;
        }
        private bool CanChangePin()
        {
            return !IsBusy &&
                IsValidPin(CurrentPin) &&
                IsValidPin(NewPin) &&
                IsValidPin(ConfirmNewPin);
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
        }
        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
        private void RefreshCommands()
        {
            ChangePinCommand.RaiseCanExecuteChanged();
            ClearCommand.RaiseCanExecuteChanged();
        }
    }
}
