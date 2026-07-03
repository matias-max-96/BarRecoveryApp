using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IDashboardService _dashboardService;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private int _totalBars;
        private int _activeBars;
        private int _inactiveBars;
        private int _disposedBars;
        private int _readyToShipBars;
        private int _shippedBars;
        private int _returnedBars;
        private int _totalRecoveryReports;
        private int _totalShipments;
        private int _totalReturns;

        public DashboardViewModel(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService
                ?? throw new ArgumentNullException(nameof(dashboardService));

            Title = "Resumen operacional";

            BarsByPlant = new ObservableCollection<PlantBarSummaryDto>();
            BarsByStatus = new ObservableCollection<BarStatusSummaryDto>();

            LoadCommand = new RelayCommand(LoadAsync);
        }

        public ObservableCollection<PlantBarSummaryDto> BarsByPlant { get; }

        public ObservableCollection<BarStatusSummaryDto> BarsByStatus { get; }

        public int TotalBars
        {
            get => _totalBars;
            set => SetProperty(ref _totalBars, value);
        }

        public int ActiveBars
        {
            get => _activeBars;
            set => SetProperty(ref _activeBars, value);
        }

        public int InactiveBars
        {
            get => _inactiveBars;
            set => SetProperty(ref _inactiveBars, value);
        }

        public int DisposedBars
        {
            get => _disposedBars;
            set => SetProperty(ref _disposedBars, value);
        }

        public int ReadyToShipBars
        {
            get => _readyToShipBars;
            set => SetProperty(ref _readyToShipBars, value);
        }

        public int ShippedBars
        {
            get => _shippedBars;
            set => SetProperty(ref _shippedBars, value);
        }

        public int ReturnedBars
        {
            get => _returnedBars;
            set => SetProperty(ref _returnedBars, value);
        }

        public int TotalRecoveryReports
        {
            get => _totalRecoveryReports;
            set => SetProperty(ref _totalRecoveryReports, value);
        }

        public int TotalShipments
        {
            get => _totalShipments;
            set => SetProperty(ref _totalShipments, value);
        }

        public int TotalReturns
        {
            get => _totalReturns;
            set => SetProperty(ref _totalReturns, value);
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

                BarsByPlant.Clear();
                BarsByStatus.Clear();

                var summary = await _dashboardService.GetSummaryAsync();

                TotalBars = summary.TotalBars;
                ActiveBars = summary.ActiveBars;
                InactiveBars = summary.InactiveBars;
                DisposedBars = summary.DisposedBars;
                ReadyToShipBars = summary.ReadyToShipBars;
                ShippedBars = summary.ShippedBars;
                ReturnedBars = summary.ReturnedBars;
                TotalRecoveryReports = summary.TotalRecoveryReports;
                TotalShipments = summary.TotalShipments;
                TotalReturns = summary.TotalReturns;

                foreach (var item in summary.BarsByPlant)
                    BarsByPlant.Add(item);

                foreach (var item in summary.BarsByStatus)
                    BarsByStatus.Add(item);
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando resumen: {ex.Message}");
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