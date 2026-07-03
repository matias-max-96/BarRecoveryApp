using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ViewModels
{
    public class BarImportViewModel : BaseViewModel
    {
        private readonly IBarImportService _barImportService;

        private string _selectedFileName = string.Empty;
        private string _summaryText = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public BarImportViewModel(IBarImportService barImportService)
        {
            _barImportService = barImportService
                ?? throw new ArgumentNullException(nameof(barImportService));

            Title = "Importación de barras";

            Errors = new ObservableCollection<BarImportErrorDto>();

            PickAndImportCommand = new RelayCommand(PickAndImportAsync);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public ObservableCollection<BarImportErrorDto> Errors { get; }

        public string SelectedFileName
        {
            get => _selectedFileName;
            set => SetProperty(ref _selectedFileName, value);
        }

        public string SummaryText
        {
            get => _summaryText;
            set => SetProperty(ref _summaryText, value);
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

        public bool HasImportErrors => Errors.Count > 0;

        public RelayCommand PickAndImportCommand { get; }

        public RelayCommand ClearCommand { get; }

        private async Task PickAndImportAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();
                Errors.Clear();

                var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    {
                        DevicePlatform.Android,
                        new[]
                        {
                            "text/csv",
                            "text/comma-separated-values",
                            "application/csv",
                            "text/plain",
                            "application/octet-stream"
                        }
                    },
                    {
                        DevicePlatform.WinUI,
                        new[]
                        {
                            ".csv",
                            ".txt"
                        }
                    },
                    {
                        DevicePlatform.iOS,
                        new[]
                        {
                            "public.comma-separated-values-text",
                            "public.plain-text"
                        }
                    },
                    {
                        DevicePlatform.MacCatalyst,
                        new[]
                        {
                            "public.comma-separated-values-text",
                            "public.plain-text"
                        }
                    }
                });

                var pickOptions = new PickOptions
                {
                    PickerTitle = "Seleccione archivo CSV de barras",
                    FileTypes = fileTypes
                };

                var file = await FilePicker.Default.PickAsync(pickOptions);

                if (file is null)
                {
                    ShowError("No se seleccionó ningún archivo.");
                    return;
                }

                SelectedFileName = file.FileName;

                await using var stream = await file.OpenReadAsync();

                var result = await _barImportService.ImportFromCsvStreamAsync(
                    stream,
                    file.FileName);

                SummaryText = result.SummaryText;

                foreach (var error in result.Errors)
                {
                    Errors.Add(error);
                }

                OnPropertyChanged(nameof(HasImportErrors));

                if (result.CreatedCount > 0)
                {
                    ShowSuccess($"Importación finalizada. Barras creadas: {result.CreatedCount}.");
                }
                else if (result.ErrorCount > 0)
                {
                    ShowError("La importación finalizó con errores. Revise el detalle.");
                }
                else
                {
                    ShowError("No se crearon barras. Revise el archivo o los datos existentes.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error importando barras: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task ClearAsync()
        {
            SelectedFileName = string.Empty;
            SummaryText = string.Empty;
            Errors.Clear();

            ClearMessages();

            OnPropertyChanged(nameof(HasImportErrors));
            RefreshCommands();

            return Task.CompletedTask;
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
            PickAndImportCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(HasImportErrors));
        }
    }
}