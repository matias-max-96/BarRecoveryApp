using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ViewModels
{
    public class RecoveryWorkReportsHistoryViewModel : BaseViewModel
    {
        private readonly IRecoveryWorkReportService _service;

        private RecoveryWorkReportItemDto? _selectedReport;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        public RecoveryWorkReportsHistoryViewModel(
            IRecoveryWorkReportService service)
        {
            _service = service
                ?? throw new ArgumentNullException(nameof(service));

            Title = "Mis registros recientes";

            Reports = new ObservableCollection<RecoveryWorkReportItemDto>();

            LoadCommand = new RelayCommand(LoadAsync);
        }

        public ObservableCollection<RecoveryWorkReportItemDto> Reports { get; }

        public RecoveryWorkReportItemDto? SelectedReport
        {
            get => _selectedReport;
            set
            {
                if (SetProperty(ref _selectedReport, value))
                {
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

        public RelayCommand LoadCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                Reports.Clear();

                var reports = await _service.GetMyReportItemsAsync();

                foreach (var report in reports)
                    Reports.Add(report);
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando registros: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                LoadCommand?.RaiseCanExecuteChanged();
            }
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
    }
}