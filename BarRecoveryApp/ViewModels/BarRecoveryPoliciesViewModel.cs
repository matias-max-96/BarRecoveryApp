using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.ViewModels.Items;

namespace BarRecoveryApp.ViewModels
{
    public class BarRecoveryPoliciesViewModel : BaseViewModel
    {
        private readonly IBarRecoveryPolicyService _service;

        private BarRecoveryPolicyItemViewModel? _selectedPolicy;
        private Plant? _selectedPlant;
        private BarType? _selectedBarType;

        private string _maxRecoveries = "0";

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public BarRecoveryPoliciesViewModel(
            IBarRecoveryPolicyService service)
        {
            _service = service
                ?? throw new ArgumentNullException(nameof(service));

            Title = "Políticas de recuperación";

            Policies = new ObservableCollection<BarRecoveryPolicyItemViewModel>();
            Plants = new ObservableCollection<Plant>();
            BarTypes = new ObservableCollection<BarType>();

            LoadCommand = new RelayCommand(LoadAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ToggleActiveCommand = new RelayCommand(ToggleActiveAsync, CanSelectPolicy);
            NewCommand = new RelayCommand(NewAsync);
        }

        public ObservableCollection<BarRecoveryPolicyItemViewModel> Policies { get; }

        public ObservableCollection<Plant> Plants { get; }

        public ObservableCollection<BarType> BarTypes { get; }

        public BarRecoveryPolicyItemViewModel? SelectedPolicy
        {
            get => _selectedPolicy;
            set
            {
                if (SetProperty(ref _selectedPolicy, value))
                {
                    LoadSelectedPolicyToForm();
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

        public string MaxRecoveries
        {
            get => _maxRecoveries;
            set
            {
                if (SetProperty(ref _maxRecoveries, value))
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
            SelectedPolicy is null ? "Crear política" : "Actualizar política";

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

                Policies.Clear();
                Plants.Clear();
                BarTypes.Clear();

                var policies = await _service.GetPoliciesAsync();
                var plants = await _service.GetActivePlantsAsync();
                var barTypes = await _service.GetActiveBarTypesAsync();

                foreach (var plant in plants)
                    Plants.Add(plant);

                foreach (var barType in barTypes)
                    BarTypes.Add(barType);

                foreach (var policy in policies)
                {
                    var plant = plants.FirstOrDefault(x => x.Id == policy.PlantId);
                    var barType = barTypes.FirstOrDefault(x => x.Id == policy.BarTypeId);

                    Policies.Add(new BarRecoveryPolicyItemViewModel
                    {
                        Policy = policy,
                        PlantName = plant?.Name ?? "Planta no encontrada",
                        BarTypeName = barType?.Name ?? "Tipo no encontrado"
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando políticas: {ex.Message}");
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
                ShowError("Debe seleccionar planta, tipo de barra y una cantidad máxima válida.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var maxRecoveriesValue = int.Parse(MaxRecoveries);

                var saved = await _service.SavePolicyAsync(
                    SelectedPolicy?.Policy.Id,
                    SelectedPlant!.Id,
                    SelectedBarType!.Id,
                    maxRecoveriesValue);

                if (!saved)
                {
                    ShowError("No fue posible guardar la política. Verifique permisos o combinación duplicada.");
                    return;
                }

                ShowSuccess(SelectedPolicy is null
                    ? "Política creada correctamente."
                    : "Política actualizada correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando política: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedPolicy is null)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                var newState = !SelectedPolicy.Policy.IsActive;

                var changed = await _service.SetPolicyActiveStateAsync(
                    SelectedPolicy.Policy.Id,
                    newState);

                if (!changed)
                {
                    ShowError("No fue posible cambiar el estado de la política.");
                    return;
                }

                ShowSuccess(newState
                    ? "Política activada correctamente."
                    : "Política desactivada correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error cambiando estado: {ex.Message}");
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
                   && SelectedPlant is not null
                   && SelectedBarType is not null
                   && int.TryParse(MaxRecoveries, out var value)
                   && value >= 0;
        }

        private bool CanSelectPolicy()
        {
            return !IsBusy && SelectedPolicy is not null;
        }

        private void LoadSelectedPolicyToForm()
        {
            if (SelectedPolicy is null)
            {
                ClearFormFieldsOnly();
            }
            else
            {
                SelectedPlant = Plants.FirstOrDefault(
                    x => x.Id == SelectedPolicy.Policy.PlantId);

                SelectedBarType = BarTypes.FirstOrDefault(
                    x => x.Id == SelectedPolicy.Policy.BarTypeId);

                MaxRecoveries = SelectedPolicy.Policy.MaxRecoveries.ToString();
            }

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearForm()
        {
            SelectedPolicy = null;
            ClearFormFieldsOnly();

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearFormFieldsOnly()
        {
            SelectedPlant = null;
            SelectedBarType = null;
            MaxRecoveries = "0";
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
            SaveCommand?.RaiseCanExecuteChanged();
            ToggleActiveCommand?.RaiseCanExecuteChanged();
            NewCommand?.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(SaveButtonText));
        }
    }
}