using System.Collections.ObjectModel;
using System.Globalization;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ViewModels
{
    public class BarReturnViewModel : BaseViewModel
    {
        private readonly IBarReturnService _barReturnService;

        private const int DefaultMaxResults = 200;

        private List<BarType> _allBarTypes = new();

        private Plant? _selectedPlant;
        private BarType? _selectedBarType;

        private string _searchText = string.Empty;
        private string _returnDocument = string.Empty;
        private string _notes = string.Empty;

        private string _resultCountText = string.Empty;
        private string _selectedCountText = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public BarReturnViewModel(IBarReturnService barReturnService)
        {
            _barReturnService = barReturnService
                ?? throw new ArgumentNullException(nameof(barReturnService));

            Title = "Recepción de barras";

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
                    ApplyBarTypeFilter();
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

        public string ReturnDocument
        {
            get => _returnDocument;
            set
            {
                if (SetProperty(ref _returnDocument, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                if (SetProperty(ref _notes, value))
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

                var plants = await _barReturnService.GetActivePlantsAsync();
                var barTypes = await _barReturnService.GetActiveBarTypesAsync();

                foreach (var plant in plants.OrderBy(x => x.Name))
                    Plants.Add(plant);

                _allBarTypes = barTypes.OrderBy(x => x.Name).ToList();
                ApplyBarTypeFilter();

                ResultCountText = "Seleccione filtros o busque por número de barra.";
                UpdateSelectedCountText();
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando recepción: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
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

                var results = await _barReturnService.SearchShippedBarsAsync(
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
                    ResultCountText = "No se encontraron barras enviadas.";
                }
                else if (Bars.Count >= DefaultMaxResults)
                {
                    ResultCountText = $"Resultados: {Bars.Count} barra(s). Refine la búsqueda.";
                }
                else
                {
                    ResultCountText = $"Resultados: {Bars.Count} barra(s).";
                }

                UpdateSelectedCountText();
            }
            catch (Exception ex)
            {
                ShowError($"Error buscando barras enviadas: {ex.Message}");
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
                    bar.WeightKgText = string.Empty;
                    bar.PropertyChanged += OnSelectedBarPropertyChanged;
                    SelectedBars.Add(bar);
                }
            }
            else
            {
                var existing = SelectedBars.FirstOrDefault(x => x.BarId == bar.BarId);

                if (existing is not null)
                {
                    existing.PropertyChanged -= OnSelectedBarPropertyChanged;
                    existing.WeightKgText = string.Empty;
                    SelectedBars.Remove(existing);
                }
            }

            UpdateSelectedCountText();
            ClearMessages();
            RefreshCommands();
        }

        // El peso es obligatorio por barra — cada vez que cambia, hay que
        // reevaluar si "Registrar recepción" puede habilitarse.
        private void OnSelectedBarPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShipmentBarTargetDto.WeightKgText))
            {
                ClearMessages();
                RefreshCommands();
            }
        }

        public void RemoveSelectedBar(ShipmentBarTargetDto bar)
        {
            var existing = SelectedBars.FirstOrDefault(x => x.BarId == bar.BarId);

            if (existing is not null)
            {
                existing.PropertyChanged -= OnSelectedBarPropertyChanged;
                existing.WeightKgText = string.Empty;
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
                ShowError(SelectedBars.Count == 0
                    ? "Debe seleccionar al menos una barra enviada."
                    : "Debe ingresar el peso de todas las barras seleccionadas.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var weightInputs = new List<BarReturnWeightInputDto>();

                foreach (var bar in SelectedBars)
                {
                    if (!TryParseWeight(bar.WeightKgText, out var weightKg))
                    {
                        ShowError($"El peso ingresado para la barra {bar.BarNumber} no es válido.");
                        return;
                    }

                    weightInputs.Add(new BarReturnWeightInputDto
                    {
                        BarId = bar.BarId,
                        WeightKg = weightKg
                    });
                }

                var result = await _barReturnService.CreateReturnReceiptAsync(
                    ReturnDocument,
                    Notes,
                    weightInputs);

                if (!result.Success)
                {
                    ShowError(result.ErrorMessage ?? "No fue posible registrar la recepción.");
                    return;
                }

                var successMessage = "Recepción registrada correctamente.";

                if (result.DisposedBarNumbers.Count > 0)
                {
                    successMessage += " Dada(s) de baja automáticamente por peso bajo el mínimo: " +
                        string.Join(", ", result.DisposedBarNumbers) + ".";
                }

                ShowSuccess(successMessage);

                ReturnDocument = string.Empty;
                Notes = string.Empty;
                UnsubscribeSelectedBars();
                SelectedBars.Clear();
                Bars.Clear();

                UpdateSelectedCountText();

                await SearchAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error registrando recepción: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private void ApplyBarTypeFilter()
        {
            var filtered = PlantBarTypeRestriction.Filter(SelectedPlant, _allBarTypes);

            BarTypes.Clear();
            foreach (var barType in filtered)
                BarTypes.Add(barType);

            if (SelectedBarType is not null && !BarTypes.Any(x => x.Id == SelectedBarType.Id))
            {
                SelectedBarType = null;
            }
        }

        private void UnsubscribeSelectedBars()
        {
            foreach (var bar in SelectedBars)
            {
                bar.PropertyChanged -= OnSelectedBarPropertyChanged;
            }
        }

        private static bool TryParseWeight(string? text, out double weightKg)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                weightKg = 0;
                return false;
            }

            // Acepta tanto punto como coma decimal — el teclado numérico del
            // dispositivo puede usar cualquiera según el idioma configurado.
            if (double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out weightKg))
                return weightKg > 0;

            if (double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out weightKg))
                return weightKg > 0;

            return false;
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

            UnsubscribeSelectedBars();
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
            ReturnDocument = string.Empty;
            Notes = string.Empty;

            foreach (var bar in Bars)
                bar.IsSelected = false;

            Bars.Clear();
            UnsubscribeSelectedBars();
            SelectedBars.Clear();

            ResultCountText = string.Empty;

            UpdateSelectedCountText();
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private bool CanSave()
        {
            if (IsBusy || SelectedBars.Count == 0)
                return false;

            // El pesaje es obligatorio para el 100% de las barras
            // recepcionadas — no se habilita "Registrar recepción" hasta
            // que todas tengan un peso válido cargado.
            return SelectedBars.All(x => TryParseWeight(x.WeightKgText, out _));
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

            OnPropertyChanged(nameof(ResultCountText));
            OnPropertyChanged(nameof(SelectedCountText));
        }
    }
}