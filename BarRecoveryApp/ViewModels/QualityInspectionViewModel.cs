using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.ViewModels.Items;
using System.Collections.ObjectModel;

namespace BarRecoveryApp.ViewModels
{
    public class QualityInspectionViewModel : BaseViewModel
    {
        private readonly IQualityInspectionService _service;

        private const int DefaultMaxResults = 200;

        private Plant? _selectedPlant;
        private BarType? _selectedBarType;
        private BarInspectionTargetDto? _selectedBar;

        private string _searchText = string.Empty;
        private string _recoveryCountFilter = string.Empty;
        private bool _includeDisposed;

        private string _recoveryCount = "0";
        private bool _canBeRecovered;
        private bool _mustBeDisposed;
        private bool _isApprovedForShipment;
        private string _notes = string.Empty;

        private string _resultCountText = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public QualityInspectionViewModel(IQualityInspectionService service)
        {
            _service = service
                ?? throw new ArgumentNullException(nameof(service));

            Title = "Inspección de calidad";

            Plants = new ObservableCollection<Plant>();
            BarTypes = new ObservableCollection<BarType>();
            Bars = new ObservableCollection<BarInspectionTargetDto>();
            TechnicalAttributes = new ObservableCollection<QualityInspectionAttributeItemViewModel>();

            LoadCommand = new RelayCommand(LoadAsync);
            SearchCommand = new RelayCommand(SearchAsync, CanSearch);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ClearCommand = new RelayCommand(ClearAsync);
            ClearFiltersCommand = new RelayCommand(ClearFiltersAsync);
        }

        public ObservableCollection<Plant> Plants { get; }

        public ObservableCollection<BarType> BarTypes { get; }

        public ObservableCollection<BarInspectionTargetDto> Bars { get; }

        public ObservableCollection<QualityInspectionAttributeItemViewModel> TechnicalAttributes { get; }

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

        public BarInspectionTargetDto? SelectedBar
        {
            get => _selectedBar;
            set
            {
                if (SetProperty(ref _selectedBar, value))
                {
                    _ = LoadSelectedBarAsync();
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
        public string RecoveryCountFilter
        {
            get => _recoveryCountFilter;
            set
            {
                if (SetProperty(ref _recoveryCountFilter, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }
        public bool IncludeDisposed
        {
            get => _includeDisposed;
            set
            {
                if (SetProperty(ref _includeDisposed, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string RecoveryCount
        {
            get => _recoveryCount;
            set
            {
                if (SetProperty(ref _recoveryCount, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public bool CanBeRecovered
        {
            get => _canBeRecovered;
            set
            {
                if (SetProperty(ref _canBeRecovered, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public bool MustBeDisposed
        {
            get => _mustBeDisposed;
            set
            {
                if (SetProperty(ref _mustBeDisposed, value))
                {
                    if (value)
                    {
                        IsApprovedForShipment = false;
                    }

                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public bool IsApprovedForShipment
        {
            get => _isApprovedForShipment;
            set
            {
                if (SetProperty(ref _isApprovedForShipment, value))
                {
                    if (value)
                    {
                        MustBeDisposed = false;
                    }

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

        public string SelectedBarInfo =>
            SelectedBar is null
                ? "Seleccione una barra para inspeccionar."
                : $"{SelectedBar.DisplayName} | {SelectedBar.PolicyText}";

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

        public RelayCommand ClearCommand { get; }

        public RelayCommand ClearFiltersCommand { get; }

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

                SelectedPlant = null;
                SelectedBarType = null;
                SelectedBar = null;
                SearchText = string.Empty;
                RecoveryCountFilter = string.Empty;
                IncludeDisposed = false;
                ResultCountText = string.Empty;

                var plants = await _service.GetActivePlantsAsync();
                var barTypes = await _service.GetActiveBarTypesAsync();

                foreach (var plant in plants.OrderBy(x => x.Name))
                {
                    Plants.Add(plant);
                }

                foreach (var barType in barTypes.OrderBy(x => x.Name))
                {
                    BarTypes.Add(barType);
                }

                ResultCountText = "Seleccione filtros o busque por número de barra.";
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando pantalla de inspección: {ex.Message}");
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
                ClearInspectionFormOnly();

                int? recoveryCountFilter = null;

                if (!string.IsNullOrWhiteSpace(RecoveryCountFilter))
                {
                    if (!int.TryParse(RecoveryCountFilter, out var parsedRecoveryCount) ||
                        parsedRecoveryCount < 0)
                    {
                        ShowError("El filtro de recuperaciones debe ser un número válido mayor o igual a 0.");
                        return;
                    }

                    recoveryCountFilter = parsedRecoveryCount;
                }

                var results = await _service.SearchBarsForInspectionAsync(
                                                SelectedPlant?.Id,
                                                SelectedBarType?.Id,
                                                SearchText,
                                                null,
                                                IncludeDisposed,
                                                recoveryCountFilter,
                                                DefaultMaxResults);

                foreach (var bar in results)
                {
                    Bars.Add(bar);
                }

                if (Bars.Count == 0)
                {
                    ResultCountText = "No se encontraron barras con los filtros seleccionados.";
                }
                else if (Bars.Count >= DefaultMaxResults)
                {
                    ResultCountText = $"Resultados: {Bars.Count} barra(s). Refine la búsqueda para ver resultados más específicos.";
                }
                else
                {
                    ResultCountText = $"Resultados: {Bars.Count} barra(s).";
                }
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

        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            if (!CanSave())
            {
                ShowError("Debe seleccionar una barra e ingresar recuperaciones válidas.");
                return;
            }
            var attributeValues = BuildAttributeValueInputs();

            if (attributeValues is null)
                return;
            try
            {
                IsBusy = true;
                ClearMessages();

                var count = int.Parse(RecoveryCount);

                var saved = await _service.CreateInspectionAsync(
                    SelectedBar!.BarId,
                    count,
                    CanBeRecovered,
                    MustBeDisposed,
                    IsApprovedForShipment,
                    Notes,
                    attributeValues);

                if (!saved)
                {
                    ShowError("No fue posible guardar la inspección. Verifique permisos o datos ingresados.");
                    return;
                }

                ShowSuccess("Inspección guardada correctamente.");

                ClearInspectionFormOnly();

                await SearchAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando inspección: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task ClearAsync()
        {
            ClearInspectionFormOnly();
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task ClearFiltersAsync()
        {
            SelectedPlant = null;
            SelectedBarType = null;
            SearchText = string.Empty;
            RecoveryCountFilter = string.Empty;
            IncludeDisposed = false;

            Bars.Clear();
            ResultCountText = "Seleccione filtros o busque por número de barra.";

            ClearInspectionFormOnly();
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private bool CanSearch()
        {
            return !IsBusy;
        }

        private bool CanSave()
        {
            return !IsBusy
                   && SelectedBar is not null
                   && int.TryParse(RecoveryCount, out var count)
                   && count >= 0
                   && !(MustBeDisposed && IsApprovedForShipment);
        }

        private async Task LoadSelectedBarAsync()
        {
            TechnicalAttributes.Clear();

            if (SelectedBar is null)
            {
                RecoveryCount = "0";
                CanBeRecovered = false;
                MustBeDisposed = false;
                IsApprovedForShipment = false;
                Notes = string.Empty;
            }
            else
            {
                RecoveryCount = SelectedBar.CurrentRecoveryCount.ToString();
                CanBeRecovered = false;
                MustBeDisposed = false;
                IsApprovedForShipment = false;
                Notes = string.Empty;

                var definitions = await _service.GetApplicableAttributeDefinitionsAsync(
                    SelectedBar.BarId);

                foreach (var definition in definitions)
                {
                    TechnicalAttributes.Add(new QualityInspectionAttributeItemViewModel
                    {
                        Definition = definition,
                        WasMeasured = definition.IsRequired

                    });
                }
            }

            OnPropertyChanged(nameof(SelectedBarInfo));
        }

        private void ClearInspectionFormOnly()
        {
            SelectedBar = null;
            RecoveryCount = "0";
            CanBeRecovered = false;
            MustBeDisposed = false;
            IsApprovedForShipment = false;
            Notes = string.Empty;
            TechnicalAttributes.Clear();

            OnPropertyChanged(nameof(SelectedBarInfo));
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
            ClearCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(SelectedBarInfo));
            OnPropertyChanged(nameof(ResultCountText));
        }
        private List<QualityInspectionAttributeValueInputDto>? BuildAttributeValueInputs()
        {
            var result = new List<QualityInspectionAttributeValueInputDto>();

            foreach (var attribute in TechnicalAttributes)
            {
                if (attribute.IsRequired && !attribute.WasMeasured)
                {
                    ShowError($"El atributo {attribute.Name} es obligatorio.");
                    return null;
                }

                if (!attribute.WasMeasured)
                {
                    result.Add(new QualityInspectionAttributeValueInputDto
                    {
                        AttributeDefinitionId = attribute.AttributeDefinitionId,
                        WasMeasured = false
                    });

                    continue;
                }

                if (attribute.IsNumeric)
                {
                    if (!attribute.TryGetNumericValue(out var numericValue))
                    {
                        if (attribute.IsRequired)
                        {
                            ShowError($"Debe ingresar un valor numérico para {attribute.Name}.");
                            return null;
                        }

                        continue;
                    }

                    result.Add(new QualityInspectionAttributeValueInputDto
                    {
                        AttributeDefinitionId = attribute.AttributeDefinitionId,
                        WasMeasured = true,
                        ValueNumber = numericValue,
                        ValueText = attribute.ValueText
                    });

                    continue;
                }

                if (attribute.IsBoolean)
                {
                    if (!attribute.ValueBool.HasValue)
                    {
                        ShowError($"Debe seleccionar Sí o No para {attribute.Name}, o desmarcar la prueba.");
                        return null;
                    }

                    result.Add(new QualityInspectionAttributeValueInputDto
                    {
                        AttributeDefinitionId = attribute.AttributeDefinitionId,
                        WasMeasured = true,
                        ValueBool = attribute.ValueBool
                    });

                    continue;
                }

                if (attribute.IsText)
                {
                    if (attribute.IsRequired &&
                        string.IsNullOrWhiteSpace(attribute.ValueText))
                    {
                        ShowError($"Debe ingresar un valor para {attribute.Name}.");
                        return null;
                    }

                    result.Add(new QualityInspectionAttributeValueInputDto
                    {
                        AttributeDefinitionId = attribute.AttributeDefinitionId,
                        WasMeasured = true,
                        ValueText = attribute.ValueText
                    });

                    continue;
                }

                result.Add(new QualityInspectionAttributeValueInputDto
                {
                    AttributeDefinitionId = attribute.AttributeDefinitionId,
                    WasMeasured = attribute.WasMeasured,
                    ValueText = attribute.ValueText
                });
            }

            return result;
        }
    }
}