using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ViewModels
{
    public class PlantsViewModel : BaseViewModel
    {
        private readonly IPlantService _plantService;

        private Plant? _selectedPlant;

        private string _code = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public PlantsViewModel(IPlantService plantService)
        {
            _plantService = plantService
                ?? throw new ArgumentNullException(nameof(plantService));

            Title = "Plantas";

            Plants = new ObservableCollection<Plant>();

            LoadCommand = new RelayCommand(LoadAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ToggleActiveCommand = new RelayCommand(ToggleActiveAsync, CanSelectPlant);
            NewCommand = new RelayCommand(NewAsync);
        }

        public ObservableCollection<Plant> Plants { get; }

        public Plant? SelectedPlant
        {
            get => _selectedPlant;
            set
            {
                if (SetProperty(ref _selectedPlant, value))
                {
                    LoadSelectedPlantToForm();
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

        public string Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
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
            SelectedPlant is null ? "Crear planta" : "Actualizar planta";

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

                Plants.Clear();

                var plants = await _plantService.GetPlantsAsync();

                foreach (var plant in plants)
                    Plants.Add(plant);
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando plantas: {ex.Message}");
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
                ShowError("Debe ingresar código y nombre de la planta.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var saved = await _plantService.SavePlantAsync(
                    SelectedPlant?.Id,
                    Code,
                    Name,
                    Description);

                if (!saved)
                {
                    ShowError("No fue posible guardar la planta. Verifique permisos o código duplicado.");
                    return;
                }

                ShowSuccess(SelectedPlant is null
                    ? "Planta creada correctamente."
                    : "Planta actualizada correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando planta: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedPlant is null)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                var newState = !SelectedPlant.IsActive;

                var changed = await _plantService.SetPlantActiveStateAsync(
                    SelectedPlant.Id,
                    newState);

                if (!changed)
                {
                    ShowError("No fue posible cambiar el estado de la planta.");
                    return;
                }

                ShowSuccess(newState
                    ? "Planta activada correctamente."
                    : "Planta desactivada correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error cambiando estado de planta: {ex.Message}");
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
                   && !string.IsNullOrWhiteSpace(Name);
        }

        private bool CanSelectPlant()
        {
            return !IsBusy && SelectedPlant is not null;
        }

        private void LoadSelectedPlantToForm()
        {
            if (SelectedPlant is null)
            {
                Code = string.Empty;
                Name = string.Empty;
                Description = string.Empty;
            }
            else
            {
                Code = SelectedPlant.Code;
                Name = SelectedPlant.Name;
                Description = SelectedPlant.Description ?? string.Empty;
            }

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearForm()
        {
            SelectedPlant = null;
            Code = string.Empty;
            Name = string.Empty;
            Description = string.Empty;

            OnPropertyChanged(nameof(SaveButtonText));
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