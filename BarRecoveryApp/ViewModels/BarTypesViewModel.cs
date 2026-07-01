using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ViewModels
{
    public class BarTypesViewModel : BaseViewModel
    {
        private readonly IBarTypeService _barTypeService;

        private BarType? _selectedBarType;

        private string _code = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public BarTypesViewModel(IBarTypeService barTypeService)
        {
            _barTypeService = barTypeService
                ?? throw new ArgumentNullException(nameof(barTypeService));

            Title = "Tipos de barra";

            BarTypes = new ObservableCollection<BarType>();

            LoadCommand = new RelayCommand(LoadAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ToggleActiveCommand = new RelayCommand(ToggleActiveAsync, CanSelectBarType);
            NewCommand = new RelayCommand(NewAsync);
        }

        public ObservableCollection<BarType> BarTypes { get; }

        public BarType? SelectedBarType
        {
            get => _selectedBarType;
            set
            {
                if (SetProperty(ref _selectedBarType, value))
                {
                    LoadSelectedBarTypeToForm();
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
            SelectedBarType is null ? "Crear tipo" : "Actualizar tipo";

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

                BarTypes.Clear();

                var barTypes = await _barTypeService.GetBarTypesAsync();

                foreach (var barType in barTypes)
                    BarTypes.Add(barType);
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando tipos de barra: {ex.Message}");
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
                ShowError("Debe ingresar código y nombre del tipo de barra.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var saved = await _barTypeService.SaveBarTypeAsync(
                    SelectedBarType?.Id,
                    Code,
                    Name,
                    Description);

                if (!saved)
                {
                    ShowError("No fue posible guardar el tipo de barra. Verifique permisos o código duplicado.");
                    return;
                }

                ShowSuccess(SelectedBarType is null
                    ? "Tipo de barra creado correctamente."
                    : "Tipo de barra actualizado correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando tipo de barra: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedBarType is null)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                var newState = !SelectedBarType.IsActive;

                var changed = await _barTypeService.SetBarTypeActiveStateAsync(
                    SelectedBarType.Id,
                    newState);

                if (!changed)
                {
                    ShowError("No fue posible cambiar el estado del tipo de barra.");
                    return;
                }

                ShowSuccess(newState
                    ? "Tipo de barra activado correctamente."
                    : "Tipo de barra desactivado correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error cambiando estado del tipo de barra: {ex.Message}");
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

        private bool CanSelectBarType()
        {
            return !IsBusy && SelectedBarType is not null;
        }

        private void LoadSelectedBarTypeToForm()
        {
            if (SelectedBarType is null)
            {
                Code = string.Empty;
                Name = string.Empty;
                Description = string.Empty;
            }
            else
            {
                Code = SelectedBarType.Code;
                Name = SelectedBarType.Name;
                Description = SelectedBarType.Description ?? string.Empty;
            }

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearForm()
        {
            SelectedBarType = null;
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