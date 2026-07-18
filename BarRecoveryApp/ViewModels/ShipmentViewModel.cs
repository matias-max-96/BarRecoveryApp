using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ViewModels
{
    public class ShipmentViewModel : BaseViewModel
    {
        private readonly IShipmentService _shipmentService;

        private const int DefaultMaxResults = 200;

        private Plant? _selectedPlant;
        private BarType? _selectedBarType;

        private string _searchText = string.Empty;
        private string _transferOrder = string.Empty;
        private string _customerReference = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        private string _resultCountText = string.Empty;
        private string _selectedCountText = string.Empty;

        public ShipmentViewModel(IShipmentService shipmentService)
        {
            _shipmentService = shipmentService
                ?? throw new ArgumentNullException(nameof(shipmentService));

            Title = "Envíos";

            Plants = new ObservableCollection<Plant>();
            BarTypes = new ObservableCollection<BarType>();
            Bars = new ObservableCollection<ShipmentBarTargetDto>();
            SelectedBars = new ObservableCollection<ShipmentBarTargetDto>();

            LoadCommand = new RelayCommand(LoadAsync);
            SearchCommand = new RelayCommand(SearchAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ClearFiltersCommand = new RelayCommand(ClearFiltersAsync);
            ClearSelectionCommand = new RelayCommand(ClearSelectionAsync);
            ClearAllCommand = new RelayCommand(ClearAllAsync);
            ShareTechnicalReportCommand = new RelayCommand(ShareTechnicalReportAsync, CanShareTechnicalReport);
        }

        public ObservableCollection<Plant> Plants { get; }

        public ObservableCollection<BarType> BarTypes { get; }

        public ObservableCollection<ShipmentBarTargetDto> Bars { get; }

        public ObservableCollection<ShipmentBarTargetDto> SelectedBars { get; }

        public Plant? SelectedPlant
        {
            get => _selectedPlant;
            set
            {
                if (SetProperty(ref _selectedPlant, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public BarType? SelectedBarType
        {
            get => _selectedBarType;
            set
            {
                if (SetProperty(ref _selectedBarType, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string TransferOrder
        {
            get => _transferOrder;
            set
            {
                if (SetProperty(ref _transferOrder, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string CustomerReference
        {
            get => _customerReference;
            set
            {
                if (SetProperty(ref _customerReference, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string ResultCountText
        {
            get => _resultCountText;
            set => SetProperty(ref _resultCountText, value);
        }

        public string SelectedCountText
        {
            get => _selectedCountText;
            set => SetProperty(ref _selectedCountText, value);
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

        public RelayCommand SearchCommand { get; }

        public RelayCommand SaveCommand { get; }

        public RelayCommand ClearFiltersCommand { get; }

        public RelayCommand ClearSelectionCommand { get; }

        public RelayCommand ClearAllCommand { get; }

        public RelayCommand ShareTechnicalReportCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                Plants.Clear();
                BarTypes.Clear();
                Bars.Clear();

                var plants = await _shipmentService.GetActivePlantsAsync();
                var barTypes = await _shipmentService.GetActiveBarTypesAsync();

                foreach (var plant in plants.OrderBy(x => x.Name))
                    Plants.Add(plant);

                foreach (var barType in barTypes.OrderBy(x => x.Name))
                    BarTypes.Add(barType);

                ResultCountText = "Seleccione filtros o busque por número de barra.";
                UpdateSelectedCountText();
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando pantalla de envíos: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }
        private string _lastTechnicalReportFileName = string.Empty;
        private string _lastTechnicalReportFilePath = string.Empty;

        public string LastTechnicalReportFileName
        {
            get => _lastTechnicalReportFileName;
            set => SetProperty(ref _lastTechnicalReportFileName, value);
        }

        public string LastTechnicalReportFilePath
        {
            get => _lastTechnicalReportFilePath;
            set
            {
                if (SetProperty(ref _lastTechnicalReportFilePath, value))
                {
                    OnPropertyChanged(nameof(HasTechnicalReport));
                    ShareTechnicalReportCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasTechnicalReport
        {
            get
            {
                return !string.IsNullOrWhiteSpace(LastTechnicalReportFilePath) &&
                       File.Exists(LastTechnicalReportFilePath);
            }
        }
        private async Task SearchAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                Bars.Clear();

                var results = await _shipmentService.SearchBarsReadyToShipAsync(
                    SelectedPlant?.Id,
                    SelectedBarType?.Id,
                    SearchText,
                    DefaultMaxResults);

                foreach (var bar in results)
                {
                    var alreadySelected = SelectedBars.Any(x => x.BarId == bar.BarId);
                    bar.IsSelected = alreadySelected;

                    Bars.Add(bar);
                }

                if (Bars.Count == 0)
                {
                    ResultCountText = "No se encontraron barras listas para envío.";
                }
                else if (Bars.Count >= DefaultMaxResults)
                {
                    ResultCountText = $"Resultados: {Bars.Count} barra(s). Refine la búsqueda para ver resultados más específicos.";
                }
                else
                {
                    ResultCountText = $"Resultados: {Bars.Count} barra(s).";
                }

                UpdateSelectedCountText();
            }
            catch (Exception ex)
            {
                ShowError($"Error buscando barras: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        public void NotifyBarSelectionChanged(ShipmentBarTargetDto bar)
        {
            if (bar.IsSelected)
            {
                if (!SelectedBars.Any(x => x.BarId == bar.BarId))
                {
                    SelectedBars.Add(bar);
                }
            }
            else
            {
                var existing = SelectedBars.FirstOrDefault(x => x.BarId == bar.BarId);

                if (existing is not null)
                {
                    SelectedBars.Remove(existing);
                }
            }

            UpdateSelectedCountText();
            ClearMessages();
            RefreshCommands();
        }

        public void RemoveSelectedBar(ShipmentBarTargetDto bar)
        {
            var existing = SelectedBars.FirstOrDefault(x => x.BarId == bar.BarId);

            if (existing is not null)
            {
                SelectedBars.Remove(existing);
            }

            var resultItem = Bars.FirstOrDefault(x => x.BarId == bar.BarId);

            if (resultItem is not null)
            {
                resultItem.IsSelected = false;
            }

            UpdateSelectedCountText();
            RefreshCommands();
        }

        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            if (!CanSave())
            {
                ShowError("Debe ingresar orden de traslado y seleccionar al menos una barra.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var selectedBarIds = SelectedBars
                    .Select(x => x.BarId)
                    .Distinct()
                    .ToList();

                var saved = await _shipmentService.CreateShipmentAsync(
                    TransferOrder,
                    CustomerReference,
                    selectedBarIds);

                if (!saved.Success)
                {
                    LastTechnicalReportFileName = string.Empty;
                    LastTechnicalReportFilePath = string.Empty;

                    ShowError(saved.Message);
                    return;
                }

                LastTechnicalReportFileName = saved.TechnicalReportFileName;
                LastTechnicalReportFilePath = saved.TechnicalReportFilePath;


                ShowSuccess(saved.Message);

                TransferOrder = string.Empty;
                CustomerReference = string.Empty;

                SelectedBars.Clear();
                Bars.Clear();

                UpdateSelectedCountText();

                await SearchAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error creando envío: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task ClearFiltersAsync()
        {
            SelectedPlant = null;
            SelectedBarType = null;
            SearchText = string.Empty;

            Bars.Clear();

            ResultCountText = "Seleccione filtros o busque por número de barra.";

            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task ClearSelectionAsync()
        {
            foreach (var bar in Bars)
                bar.IsSelected = false;

            SelectedBars.Clear();

            UpdateSelectedCountText();
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task ClearAllAsync()
        {
            SelectedPlant = null;
            SelectedBarType = null;
            SearchText = string.Empty;
            TransferOrder = string.Empty;
            CustomerReference = string.Empty;

            foreach (var bar in Bars)
                bar.IsSelected = false;

            Bars.Clear();
            SelectedBars.Clear();

            ResultCountText = string.Empty;

            UpdateSelectedCountText();
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private bool CanSave()
        {
            return !IsBusy
                   && !string.IsNullOrWhiteSpace(TransferOrder)
                   && SelectedBars.Count > 0;
        }

        private void UpdateSelectedCountText()
        {
            SelectedCountText = SelectedBars.Count == 0
                ? "No hay barras seleccionadas."
                : $"Barras seleccionadas: {SelectedBars.Count}";
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
            LoadCommand?.RaiseCanExecuteChanged();
            SearchCommand?.RaiseCanExecuteChanged();
            SaveCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            ClearSelectionCommand?.RaiseCanExecuteChanged();
            ClearAllCommand?.RaiseCanExecuteChanged();
            ShareTechnicalReportCommand?.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(ResultCountText));
            OnPropertyChanged(nameof(SelectedCountText));
            OnPropertyChanged(nameof(HasTechnicalReport));
        }
        private async Task ShareTechnicalReportAsync()
        {
            if (!CanShareTechnicalReport())
            {
                ShowError("No existe un reporte técnico generado para compartir.");
                return;
            }

            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartir reporte técnico de envío",
                    File = new ShareFile(LastTechnicalReportFilePath)
                });
            }
            catch (Exception ex)
            {
                ShowError($"Error compartiendo reporte técnico: {ex.Message}");
            }
        }

        private bool CanShareTechnicalReport()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(LastTechnicalReportFilePath) &&
                   File.Exists(LastTechnicalReportFilePath);
        }
    }
}