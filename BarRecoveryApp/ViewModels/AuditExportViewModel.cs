using BarRecoveryApp.ApplicationF.Services.Auditing;

namespace BarRecoveryApp.ViewModels
{
    public class AuditExportViewModel : BaseViewModel
    {
        private readonly IAuditLogService _auditLogService;

        private DateTime _fromDate = DateTime.Now.Date;
        private DateTime _toDate = DateTime.Now.Date;

        private string _lastFileName = string.Empty;
        private string _lastFilePath = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public AuditExportViewModel(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService
                ?? throw new ArgumentNullException(nameof(auditLogService));

            Title = "Exportar auditoría";

            ExportCommand = new RelayCommand(ExportAsync);
            ShareCommand = new RelayCommand(ShareAsync, CanShare);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string LastFileName
        {
            get => _lastFileName;
            set => SetProperty(ref _lastFileName, value);
        }

        public string LastFilePath
        {
            get => _lastFilePath;
            set
            {
                if (SetProperty(ref _lastFilePath, value))
                    RefreshCommands();
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

        public RelayCommand ExportCommand { get; }

        public RelayCommand ShareCommand { get; }

        public RelayCommand ClearCommand { get; }

        private async Task ExportAsync()
        {
            if (IsBusy)
                return;

            if (ToDate.Date < FromDate.Date)
            {
                ShowError("La fecha hasta no puede ser menor que la fecha desde.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var result = await _auditLogService.ExportCsvAsync(
                    FromDate,
                    ToDate);

                if (!result.Success)
                {
                    LastFileName = string.Empty;
                    LastFilePath = string.Empty;
                    ShowError(result.Message);
                    return;
                }

                LastFileName = result.FileName;
                LastFilePath = result.FilePath;

                ShowSuccess(result.Message);
            }
            catch (Exception ex)
            {
                ShowError($"Error exportando auditoría: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ShareAsync()
        {
            if (!CanShare())
            {
                ShowError("No existe un archivo de auditoría generado para compartir.");
                return;
            }

            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartir auditoría",
                    File = new ShareFile(LastFilePath)
                });
            }
            catch (Exception ex)
            {
                ShowError($"Error compartiendo auditoría: {ex.Message}");
            }
        }

        private Task ClearAsync()
        {
            LastFileName = string.Empty;
            LastFilePath = string.Empty;
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private bool CanShare()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(LastFilePath) &&
                   File.Exists(LastFilePath);
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
            ExportCommand?.RaiseCanExecuteChanged();
            ShareCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();
        }
    }
}