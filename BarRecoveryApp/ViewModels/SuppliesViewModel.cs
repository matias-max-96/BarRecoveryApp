using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ViewModels
{
    public class SuppliesViewModel : BaseViewModel
    {
        private readonly ISupplyService _supplyService;

        private Supply? _selectedSupply;

        private string _code = string.Empty;
        private string _name = string.Empty;
        private string _unit = string.Empty;
        private string _description = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public SuppliesViewModel(ISupplyService supplyService)
        {
            _supplyService = supplyService
                ?? throw new ArgumentNullException(nameof(supplyService));

            Title = "Insumos";

            Supplies = new ObservableCollection<Supply>();

            LoadCommand = new RelayCommand(LoadAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ToggleActiveCommand = new RelayCommand(ToggleActiveAsync, CanSelectSupply);
            NewCommand = new RelayCommand(NewAsync);
        }

        public ObservableCollection<Supply> Supplies { get; }

        public Supply? SelectedSupply
        {
            get => _selectedSupply;
            set
            {
                if (SetProperty(ref _selectedSupply, value))
                {
                    LoadSelectedSupplyToForm();
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
            SelectedSupply is null ? "Crear insumo" : "Actualizar insumo";

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

                Supplies.Clear();

                var supplies = await _supplyService.GetSuppliesAsync();

                foreach (var supply in supplies)
                    Supplies.Add(supply);
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando insumos: {ex.Message}");
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
                ShowError("Debe ingresar código, nombre y unidad del insumo.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var saved = await _supplyService.SaveSupplyAsync(
                    SelectedSupply?.Id,
                    Code,
                    Name,
                    Unit,
                    Description);

                if (!saved)
                {
                    ShowError("No fue posible guardar el insumo. Verifique permisos o código duplicado.");
                    return;
                }

                ShowSuccess(SelectedSupply is null
                    ? "Insumo creado correctamente."
                    : "Insumo actualizado correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando insumo: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedSupply is null)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                var newState = !SelectedSupply.IsActive;

                var changed = await _supplyService.SetSupplyActiveStateAsync(
                    SelectedSupply.Id,
                    newState);

                if (!changed)
                {
                    ShowError("No fue posible cambiar el estado del insumo.");
                    return;
                }

                ShowSuccess(newState
                    ? "Insumo activado correctamente."
                    : "Insumo desactivado correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error cambiando estado del insumo: {ex.Message}");
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
                   && !string.IsNullOrWhiteSpace(Unit);
        }

        private bool CanSelectSupply()
        {
            return !IsBusy && SelectedSupply is not null;
        }

        private void LoadSelectedSupplyToForm()
        {
            if (SelectedSupply is null)
            {
                Code = string.Empty;
                Name = string.Empty;
                Unit = string.Empty;
                Description = string.Empty;
            }
            else
            {
                Code = SelectedSupply.Code;
                Name = SelectedSupply.Name;
                Unit = SelectedSupply.Unit;
                Description = SelectedSupply.Description ?? string.Empty;
            }

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearForm()
        {
            SelectedSupply = null;
            Code = string.Empty;
            Name = string.Empty;
            Unit = string.Empty;
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