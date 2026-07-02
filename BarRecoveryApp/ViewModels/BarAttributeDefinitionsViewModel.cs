using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.ViewModels.Options;

namespace BarRecoveryApp.ViewModels
{
    public class BarAttributeDefinitionsViewModel : BaseViewModel
    {
        private readonly IBarAttributeDefinitionService _service;

        private BarAttributeDefinition? _selectedDefinition;
        private AttributeDataTypeOption? _selectedDataType;
        private Plant? _selectedPlant;
        private BarType? _selectedBarType;

        private string _code = string.Empty;
        private string _name = string.Empty;
        private string _unit = string.Empty;
        private string _displayOrder = "0";
        private bool _isRequired;

        private string _errorMessage = string.Empty;
        private bool _hasError;
        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public BarAttributeDefinitionsViewModel(
            IBarAttributeDefinitionService service)
        {
            _service = service
                ?? throw new ArgumentNullException(nameof(service));

            Title = "Atributos técnicos";

            Definitions = new ObservableCollection<BarAttributeDefinition>();
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

        public ObservableCollection<BarAttributeDefinition> Definitions { get; }

        public ObservableCollection<Plant> Plants { get; }

        public ObservableCollection<BarType> BarTypes { get; }

        public ObservableCollection<AttributeDataTypeOption> DataTypes { get; }

        public BarAttributeDefinition? SelectedDefinition
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

                foreach (var item in definitions)
                    Definitions.Add(item);

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
                ClearMessages();

                var order = int.Parse(DisplayOrder);

                var saved = await _service.SaveDefinitionAsync(
                    SelectedDefinition?.Id,
                    Code,
                    Name,
                    SelectedDataType!.Value,
                    Unit,
                    IsRequired,
                    SelectedPlant?.Id,
                    SelectedBarType?.Id,
                    order);

                if (!saved)
                {
                    ShowError("No fue posible guardar el atributo. Verifique permisos o código duplicado.");
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
                ClearMessages();

                var newState = !SelectedDefinition.IsActive;

                var changed = await _service.SetDefinitionActiveStateAsync(
                    SelectedDefinition.Id,
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
                Code = SelectedDefinition.Code;
                Name = SelectedDefinition.Name;
                Unit = SelectedDefinition.Unit ?? string.Empty;
                IsRequired = SelectedDefinition.IsRequired;
                DisplayOrder = SelectedDefinition.DisplayOrder.ToString();

                SelectedDataType = DataTypes.FirstOrDefault(
                    x => x.Value == SelectedDefinition.DataType);

                SelectedPlant = Plants.FirstOrDefault(
                    x => x.Id == SelectedDefinition.AppliesToPlantId);

                SelectedBarType = BarTypes.FirstOrDefault(
                    x => x.Id == SelectedDefinition.AppliesToBarTypeId);
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
            DisplayOrder = "0";
            SelectedPlant = null;
            SelectedBarType = null;
            SelectedDataType = DataTypes.FirstOrDefault(
                x => x.Value == AttributeDataType.Decimal);
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
        }
    }
}