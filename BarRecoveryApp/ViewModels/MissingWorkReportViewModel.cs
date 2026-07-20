using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ViewModels
{
    public class MissingWorkReportViewModel : BaseViewModel
    {
        private readonly IMissingWorkReportService _service;

        private DateTime _fromDate = DateTime.Now.Date.AddDays(-7);
        private DateTime _toDate = DateTime.Now.Date;

        private string _resultCountText = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError;

        public MissingWorkReportViewModel(
            IMissingWorkReportService service)
        {
            _service = service
                ?? throw new ArgumentNullException(nameof(service));

            Title = "Registros faltantes";

            MissingReports = new ObservableCollection<MissingWorkReportItemDto>();

            SearchCommand = new RelayCommand(SearchAsync);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public ObservableCollection<MissingWorkReportItemDto> MissingReports { get; }

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

        public RelayCommand SearchCommand { get; }

        public RelayCommand ClearCommand { get; }

        private async Task SearchAsync()
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
                ClearError();

                MissingReports.Clear();

                var reports = await _service.GetMissingWorkReportsAsync(
                    FromDate,
                    ToDate);

                foreach (var report in reports)
                    MissingReports.Add(report);

                ResultCountText = MissingReports.Count == 0
                    ? "No se encontraron registros faltantes en el rango seleccionado."
                    : $"Registros faltantes encontrados: {MissingReports.Count}";
            }
            catch (Exception ex)
            {
                ShowError($"Error buscando registros faltantes: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task ClearAsync()
        {
            FromDate = DateTime.Now.Date.AddDays(-7);
            ToDate = DateTime.Now.Date;

            MissingReports.Clear();
            ResultCountText = string.Empty;
            ClearError();
            RefreshCommands();

            return Task.CompletedTask;
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
            SearchCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();
        }
    }
}