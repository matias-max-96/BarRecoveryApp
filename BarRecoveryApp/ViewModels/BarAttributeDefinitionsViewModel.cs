using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.ViewModels.Items;
using BarRecoveryApp.ViewModels.Options;
using System.Collections.ObjectModel;
using System.Globalization;


namespace BarRecoveryApp.ViewModels
{
    public class BarAttributeDefinitionsViewModel : BaseViewModel
    {
        private readonly IBarAttributeDefinitionService _service;
        private readonly ICurrentUserService _currentUserService;

        private AttributeDataTypeOption? _selectedDataType;
        private Plant? _selectedPlant;
        private BarType? _selectedBarType;

        private string _code = string.Empty;
        private string _name = string.Empty;
        private string _unit = string.Empty;
        private string _displayOrder = "0";

        private bool _isRequired;

        private bool _hasRangeValidation;
        private string _minValue = string.Empty;
        private string _maxValue = string.Empty;
        private string _toleranceText = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public BarAttributeDefinitionsViewModel(
            IBarAttributeDefinitionService service,
            ICurrentUserService currentUserService)
        {
            _service = service
                ?? throw new ArgumentNullException(nameof(service));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));

            Title = "Atributos técnicos";

            Definitions = new ObservableCollection<BarAttributeDefinitionItemViewModel>();
            Plants = new ObservableCollection<Plant>();
            BarTypes = new ObservableCollection<BarType>();

            DataTypes = new ObservableCollection<AttributeDataTypeOption>
            {
                new AttributeDataTypeOption { Value = AttributeDataType.Text, Name = "Texto" },
                new AttributeDataTypeOption { Value = AttributeDataType.Integer, Name = "Entero" },
                new AttributeDataTypeOption { Value = AttributeDataType.Decimal, Name = "Decimal" },
                new AttributeDataTypeOption { Value = AttributeDataType.Boolean, Name = "Sí / No" },
                new AttributeDataTypeOption { Value = AttributeDataType.Date, Name = "Fecha" }
            };

            LoadCommand = new RelayCommand(LoadAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ToggleActiveCommand = new RelayCommand(ToggleActiveAsync, CanSelectDefinition);
            NewCommand = new RelayCommand(NewAsync);

            SelectedDataType = DataTypes.FirstOrDefault(x => x.Value == AttributeDataType.Decimal);
        }

        public ObservableCollection<BarAttributeDefinitionItemViewModel> Definitions { get; }

        public ObservableCollection<Plant> Plants { get; }

        public ObservableCollection<BarType> BarTypes { get; }

        public ObservableCollection<AttributeDataTypeOption> DataTypes { get; }

        private BarAttributeDefinitionItemViewModel? _selectedDefinition;

        public BarAttributeDefinitionItemViewModel? SelectedDefinition
        {
            get => _selectedDefinition;
            set
            {
                if (SetProperty(ref _selectedDefinition, value))
                {
                    LoadSelectedDefinitionToForm();
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public AttributeDataTypeOption? SelectedDataType
        {
            get => _selectedDataType;
            set
            {
                if (SetProperty(ref _selectedDataType, value))
                {
                    if (value is not null &&
                        value.Value != AttributeDataType.Decimal &&
                        value.Value != AttributeDataType.Integer)
                    {
                        HasRangeValidation = false;
                    }

                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

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

        public string Code
        {
            get => _code;
            set
            {
                if (SetProperty(ref _code, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public bool HasRangeValidation
        {
            get => _hasRangeValidation;
            set
            {
                if (SetProperty(ref _hasRangeValidation, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string MinValue
        {
            get => _minValue;
            set
            {
                if (SetProperty(ref _minValue, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string MaxValue
        {
            get => _maxValue;
            set
            {
                if (SetProperty(ref _maxValue, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string ToleranceText
        {
            get => _toleranceText;
            set
            {
                if (SetProperty(ref _toleranceText, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string DisplayOrder
        {
            get => _displayOrder;
            set
            {
                if (SetProperty(ref _displayOrder, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public bool IsRequired
        {
            get => _isRequired;
            set
            {
                if (SetProperty(ref _isRequired, value))
                {
                    ClearMessages();
                    RefreshCommands();
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

        public string SaveButtonText =>
            SelectedDefinition is null ? "Crear atributo" : "Actualizar atributo";

        public bool CanManageAttributes =>
            _currentUserService.HasPermission("ATTRIBUTE_MANAGE");

        public bool CannotManageAttributes => !CanManageAttributes;

        public RelayCommand LoadCommand { get; }

        public RelayCommand SaveCommand { get; }

        public RelayCommand ToggleActiveCommand { get; }

        public RelayCommand NewCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                Definitions.Clear();
                Plants.Clear();
                BarTypes.Clear();

                var definitions = await _service.GetDefinitionsAsync();
                var plants = await _service.GetActivePlantsAsync();
                var barTypes = await _service.GetActiveBarTypesAsync();

                foreach (var definition in definitions)
                {
                    var plant = plants.FirstOrDefault(x => x.Id == definition.AppliesToPlantId);
                    var barType = barTypes.FirstOrDefault(x => x.Id == definition.AppliesToBarTypeId);

                    Definitions.Add(new BarAttributeDefinitionItemViewModel
                    {
                        Definition = definition,
                        PlantName = plant?.Name ?? "General",
                        BarTypeName = barType?.Name ?? "General"
                    });
                }

                foreach (var plant in plants)
                    Plants.Add(plant);

                foreach (var barType in barTypes)
                    BarTypes.Add(barType);
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando atributos técnicos: {ex.Message}");
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
                ShowError("Debe ingresar código, nombre, tipo de dato y orden válido.");
                return;
            }

            try
            {
                IsBusy = true;
                RefreshCommands(); // deshabilita el botón de inmediato para evitar doble-tap
                ClearMessages();

                var order = int.Parse(DisplayOrder);

                double? minValue = null;
                double? maxValue = null;

                if (HasRangeValidation)
                {
                    if (SelectedDataType!.Value != AttributeDataType.Decimal &&
                        SelectedDataType.Value != AttributeDataType.Integer)
                    {
                        ShowError("La validación por rango solo aplica para atributos numéricos.");
                        return;
                    }

                    if (!TryParseDecimal(MinValue, out var parsedMin))
                    {
                        ShowError("El valor mínimo debe ser numérico.");
                        return;
                    }

                    if (!TryParseDecimal(MaxValue, out var parsedMax))
                    {
                        ShowError("El valor máximo debe ser numérico.");
                        return;
                    }

                    if (parsedMin > parsedMax)
                    {
                        ShowError("El valor mínimo no puede ser mayor que el valor máximo.");
                        return;
                    }

                    minValue = parsedMin;
                    maxValue = parsedMax;
                }

                var result = await _service.SaveDefinitionAsync(
                    SelectedDefinition?.Definition.Id,
                    Code,
                    Name,
                    SelectedDataType!.Value,
                    Unit,
                    IsRequired,
                    HasRangeValidation,
                    minValue,
                    maxValue,
                    ToleranceText,
                    SelectedPlant?.Id,
                    SelectedBarType?.Id,
                    order);

                if (result != SaveDefinitionResult.Success)
                {
                    ShowError(result switch
                    {
                        SaveDefinitionResult.NotAuthenticated =>
                            "Su sesión no es válida. Vuelva a iniciar sesión.",
                        SaveDefinitionResult.NoPermission =>
                            "No tiene permiso para gestionar atributos técnicos.",
                        SaveDefinitionResult.InvalidCode =>
                            "Debe ingresar un código válido.",
                        SaveDefinitionResult.InvalidName =>
                            "Debe ingresar un nombre válido.",
                        SaveDefinitionResult.InvalidRangeConfiguration =>
                            "La validación por rango solo aplica a Entero/Decimal, y el mínimo no puede ser mayor al máximo.",
                        SaveDefinitionResult.DuplicateCode =>
                            "Ya existe un atributo con ese código para la planta y tipo de barra seleccionados.",
                        SaveDefinitionResult.NotFound =>
                            "El atributo que intenta editar ya no existe. Actualice la lista.",
                        _ => "No fue posible guardar el atributo."
                    });
                    return;
                }

                ShowSuccess(SelectedDefinition is null
                    ? "Atributo técnico creado correctamente."
                    : "Atributo técnico actualizado correctamente.");

                ClearForm();
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando atributo técnico: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedDefinition is null)
                return;

            try
            {
                IsBusy = true;
                RefreshCommands();
                ClearMessages();

                var definition = SelectedDefinition.Definition;

                var newState = !definition.IsActive;

                var changed = await _service.SetDefinitionActiveStateAsync(
                    definition.Id,
                    newState);

                if (!changed)
                {
                    ShowError("No fue posible cambiar el estado del atributo.");
                    return;
                }

                ShowSuccess(newState
                    ? "Atributo activado correctamente."
                    : "Atributo desactivado correctamente.");

                ClearForm();
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error cambiando estado del atributo: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task NewAsync()
        {
            ClearForm();
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private bool CanSave()
        {
            return !IsBusy
                   && CanManageAttributes
                   && !string.IsNullOrWhiteSpace(Code)
                   && !string.IsNullOrWhiteSpace(Name)
                   && SelectedDataType is not null
                   && int.TryParse(DisplayOrder, out _);
        }

        private bool CanSelectDefinition()
        {
            return !IsBusy && SelectedDefinition is not null;
        }

        private void LoadSelectedDefinitionToForm()
        {
            if (SelectedDefinition is null)
            {
                ClearFormFieldsOnly();
            }
            else
            {
                var definition = SelectedDefinition.Definition;

                Code = definition.Code;
                Name = definition.Name;
                Unit = definition.Unit ?? string.Empty;
                IsRequired = definition.IsRequired;
                DisplayOrder = definition.DisplayOrder.ToString();

                HasRangeValidation = definition.HasRangeValidation;
                MinValue = definition.MinValue.HasValue
                    ? definition.MinValue.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                MaxValue = definition.MaxValue.HasValue
                    ? definition.MaxValue.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                ToleranceText = definition.ToleranceText ?? string.Empty;

                SelectedDataType = DataTypes.FirstOrDefault(
                    x => x.Value == definition.DataType);

                SelectedPlant = Plants.FirstOrDefault(
                    x => x.Id == definition.AppliesToPlantId);

                SelectedBarType = BarTypes.FirstOrDefault(
                    x => x.Id == definition.AppliesToBarTypeId);
            }

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearForm()
        {
            SelectedDefinition = null;
            ClearFormFieldsOnly();

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearFormFieldsOnly()
        {
            Code = string.Empty;
            Name = string.Empty;
            Unit = string.Empty;
            IsRequired = false;

            HasRangeValidation = false;
            MinValue = string.Empty;
            MaxValue = string.Empty;
            ToleranceText = string.Empty;

            DisplayOrder = "0";
            SelectedPlant = null;
            SelectedBarType = null;

            SelectedDataType = DataTypes.FirstOrDefault(
                x => x.Value == AttributeDataType.Decimal);
        }

        private static bool TryParseDecimal(
            string value,
            out double result)
        {
            return double.TryParse(
                value.Replace(",", "."),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out result);
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
            LoadCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            ToggleActiveCommand.RaiseCanExecuteChanged();
            NewCommand.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CanManageAttributes));
            OnPropertyChanged(nameof(CannotManageAttributes));
        }
    }
}