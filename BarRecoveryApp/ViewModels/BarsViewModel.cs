using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.ViewModels.Items;

namespace BarRecoveryApp.ViewModels
{
    public class BarsViewModel : BaseViewModel
    {
        private readonly IBarService _barService;

        private BarItemViewModel? _selectedBar;
        private Plant? _selectedPlant;
        private BarType? _selectedBarType;

        private string _barNumber = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public BarsViewModel(IBarService barService)
        {
            _barService = barService
                ?? throw new ArgumentNullException(nameof(barService));

            Title = "Barras";

            Bars = new ObservableCollection<BarItemViewModel>();
            Plants = new ObservableCollection<Plant>();
            BarTypes = new ObservableCollection<BarType>();

            LoadCommand = new RelayCommand(LoadAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ToggleActiveCommand = new RelayCommand(ToggleActiveAsync, CanSelectBar);
            NewCommand = new RelayCommand(NewAsync);
        }

        public ObservableCollection<BarItemViewModel> Bars { get; }

        public ObservableCollection<Plant> Plants { get; }

        public ObservableCollection<BarType> BarTypes { get; }

        public BarItemViewModel? SelectedBar
        {
            get => _selectedBar;
            set
            {
                if (SetProperty(ref _selectedBar, value))
                {
                    LoadSelectedBarToForm();
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

        public string BarNumber
        {
            get => _barNumber;
            set
            {
                if (SetProperty(ref _barNumber, value))
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
            SelectedBar is null ? "Crear barra" : "Actualizar barra";

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

                Bars.Clear();
                Plants.Clear();
                BarTypes.Clear();

                var bars = await _barService.GetBarsAsync();
                var plants = await _barService.GetActivePlantsAsync();
                var barTypes = await _barService.GetActiveBarTypesAsync();

                foreach (var plant in plants)
                    Plants.Add(plant);

                foreach (var barType in barTypes)
                    BarTypes.Add(barType);

                foreach (var bar in bars)
                {
                    var plant = plants.FirstOrDefault(x => x.Id == bar.PlantId);
                    var barType = barTypes.FirstOrDefault(x => x.Id == bar.BarTypeId);

                    Bars.Add(new BarItemViewModel
                    {
                        Bar = bar,
                        PlantName = plant?.Name ?? "Planta no encontrada",
                        BarTypeName = barType?.Name ?? "Tipo no encontrado"
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando barras: {ex.Message}");
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
                ShowError("Debe ingresar número de barra, planta y tipo de barra.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var saved = await _barService.SaveBarAsync(
                    SelectedBar?.Bar.Id,
                    BarNumber,
                    SelectedPlant!.Id,
                    SelectedBarType!.Id);

                if (!saved)
                {
                    ShowError("No fue posible guardar la barra. Verifique permisos o si ya existe una barra con la misma planta, tipo de barra y número.");
                    return;
                }

                ShowSuccess(SelectedBar is null
                    ? "Barra creada correctamente."
                    : "Barra actualizada correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando barra: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedBar is null)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                var newState = !SelectedBar.Bar.IsActive;

                var changed = await _barService.SetBarActiveStateAsync(
                    SelectedBar.Bar.Id,
                    newState);

                if (!changed)
                {
                    ShowError("No fue posible cambiar el estado de la barra.");
                    return;
                }

                ShowSuccess(newState
                    ? "Barra activada correctamente."
                    : "Barra desactivada correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error cambiando estado de barra: {ex.Message}");
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
                   && !string.IsNullOrWhiteSpace(BarNumber)
                   && SelectedPlant is not null
                   && SelectedBarType is not null;
        }

        private bool CanSelectBar()
        {
            return !IsBusy && SelectedBar is not null;
        }

        private void LoadSelectedBarToForm()
        {
            if (SelectedBar is null)
            {
                ClearFormFieldsOnly();
            }
            else
            {
                BarNumber = SelectedBar.Bar.BarNumber;

                SelectedPlant = Plants.FirstOrDefault(
                    x => x.Id == SelectedBar.Bar.PlantId);

                SelectedBarType = BarTypes.FirstOrDefault(
                    x => x.Id == SelectedBar.Bar.BarTypeId);
            }

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearForm()
        {
            SelectedBar = null;
            ClearFormFieldsOnly();

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearFormFieldsOnly()
        {
            BarNumber = string.Empty;
            SelectedPlant = null;
            SelectedBarType = null;
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