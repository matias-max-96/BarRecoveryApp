using BarRecoveryApp.ApplicationF.Services.Operations;

namespace BarRecoveryApp.ViewModels
{
    public class ReportExportViewModel : BaseViewModel
    {
        private readonly IReportExportService _reportExportService;

        private DateTime _fromDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _toDate = DateTime.Now;

        private string _lastFilePath = string.Empty;
        private string _lastFileName = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public ReportExportViewModel(IReportExportService reportExportService)
        {
            _reportExportService = reportExportService
                ?? throw new ArgumentNullException(nameof(reportExportService));

            Title = "Exportar reportes";

            ExportProductionCommand = new RelayCommand(ExportProductionAsync);
            ShareLastFileCommand = new RelayCommand(ShareLastFileAsync, CanShareLastFile);
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

        public string LastFilePath
        {
            get => _lastFilePath;
            set
            {
                if (SetProperty(ref _lastFilePath, value))
                {
                    RefreshCommands();
                }
            }
        }

        public string LastFileName
        {
            get => _lastFileName;
            set => SetProperty(ref _lastFileName, value);
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

        public RelayCommand ExportProductionCommand { get; }

        public RelayCommand ShareLastFileCommand { get; }

        public RelayCommand ClearCommand { get; }

        private async Task ExportProductionAsync()
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

                var result = await _reportExportService.ExportProductionWorkbookAsync(
                    FromDate,
                    ToDate);

                if (!result.Success)
                {
                    LastFilePath = string.Empty;
                    LastFileName = string.Empty;
                    ShowError(result.Message);
                    return;
                }

                LastFilePath = result.FilePath;
                LastFileName = result.FileName;
                ///data/user/0/com.companyname.barrecoveryapp/cache/reporte_produccion_20260701_20260706.xlsx
                //System.Diagnostics.Debug.WriteLine($"Sector de guardado {LastFilePath}");

                ShowSuccess(result.Message);
            }
            catch (Exception ex)
            {
                ShowError($"Error exportando reporte: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ShareLastFileAsync()
        {
            if (!CanShareLastFile())
            {
                ShowError("No existe un archivo generado para compartir.");
                return;
            }

            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartir reporte de producción",
                    File = new ShareFile(LastFilePath)
                });
            }
            catch (Exception ex)
            {
                ShowError($"Error compartiendo archivo: {ex.Message}");
            }
        }

        private Task ClearAsync()
        {
            LastFilePath = string.Empty;
            LastFileName = string.Empty;
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private bool CanShareLastFile()
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
            ExportProductionCommand?.RaiseCanExecuteChanged();
            ShareLastFileCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();
        }
    }
}