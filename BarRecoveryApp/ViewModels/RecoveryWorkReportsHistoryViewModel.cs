using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ViewModels
{
    public class RecoveryWorkReportsHistoryViewModel : BaseViewModel
    {
        private readonly IRecoveryWorkReportService _service;

        private readonly List<RecoveryWorkReportItemDto> _allReports = new();

        private RecoveryWorkReportItemDto? _selectedReport;

        private DateTime _fromDate = DateTime.Now.Date;
        private DateTime _toDate = DateTime.Now.Date;

        private string _resultCountText = string.Empty;
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
            ApplyFilterCommand = new RelayCommand(ApplyFilterAsync);
            ClearFilterCommand = new RelayCommand(ClearFilterAsync);
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

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                {
                    ClearError();
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
                    ClearError();
                    RefreshCommands();
                }
            }
        }

        public string ResultCountText
        {
            get => _resultCountText;
            set => SetProperty(ref _resultCountText, value);
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

        public RelayCommand ApplyFilterCommand { get; }

        public RelayCommand ClearFilterCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                Reports.Clear();
                _allReports.Clear();

                var reports = await _service.GetMyReportItemsAsync();

                foreach (var report in reports)
                    _allReports.Add(report);

                ApplyFilterInternal();
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando registros: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task ApplyFilterAsync()
        {
            if (ToDate.Date < FromDate.Date)
            {
                ShowError("La fecha hasta no puede ser menor que la fecha desde.");
                return Task.CompletedTask;
            }

            ClearError();
            ApplyFilterInternal();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task ClearFilterAsync()
        {
            FromDate = DateTime.Now.Date;
            ToDate = DateTime.Now.Date;

            ClearError();
            ApplyFilterInternal();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private void ApplyFilterInternal()
        {
            Reports.Clear();

            var filtered = _allReports
                .Where(x =>
                    x.WorkDate.Date >= FromDate.Date &&
                    x.WorkDate.Date <= ToDate.Date)
                .OrderByDescending(x => x.WorkDate)
                .ToList();

            foreach (var report in filtered)
                Reports.Add(report);

            ResultCountText = Reports.Count == 0
                ? "No hay registros en el rango seleccionado."
                : $"Registros encontrados: {Reports.Count}";
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
            LoadCommand?.RaiseCanExecuteChanged();
            ApplyFilterCommand?.RaiseCanExecuteChanged();
            ClearFilterCommand?.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(ResultCountText));
        }
    }
}