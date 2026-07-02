using System.Collections.ObjectModel;
using System.Globalization;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Operations;
using BarRecoveryApp.ViewModels.Items;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ViewModels
{
    public class RecoveryRecordViewModel : BaseViewModel
    {
        private readonly IRecoveryService _recoveryService;

        private Bar? _selectedBar;
        private ActivityModel? _selectedActivity;
        private Supply? _selectedSupply;
        private RecoverySupplyEntryItemViewModel? _selectedSupplyEntry;

        private string _quantity = string.Empty;
        private string _notes = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public RecoveryRecordViewModel(IRecoveryService recoveryService)
        {
            _recoveryService = recoveryService
                ?? throw new ArgumentNullException(nameof(recoveryService));

            Title = "Registrar recuperación";

            Bars = new ObservableCollection<Bar>();
            Activities = new ObservableCollection<ActivityModel>();
            Supplies = new ObservableCollection<Supply>();
            UsedSupplies = new ObservableCollection<RecoverySupplyEntryItemViewModel>();

            LoadCommand = new RelayCommand(LoadAsync);
            AddSupplyCommand = new RelayCommand(AddSupplyAsync, CanAddSupply);
            RemoveSupplyCommand = new RelayCommand(RemoveSupplyAsync, CanRemoveSupply);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public ObservableCollection<Bar> Bars { get; }

        public ObservableCollection<ActivityModel> Activities { get; }

        public ObservableCollection<Supply> Supplies { get; }

        public ObservableCollection<RecoverySupplyEntryItemViewModel> UsedSupplies { get; }

        public Bar? SelectedBar
        {
            get => _selectedBar;
            set
            {
                if (SetProperty(ref _selectedBar, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public ActivityModel? SelectedActivity
        {
            get => _selectedActivity;
            set
            {
                if (SetProperty(ref _selectedActivity, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public Supply? SelectedSupply
        {
            get => _selectedSupply;
            set
            {
                if (SetProperty(ref _selectedSupply, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public RecoverySupplyEntryItemViewModel? SelectedSupplyEntry
        {
            get => _selectedSupplyEntry;
            set
            {
                if (SetProperty(ref _selectedSupplyEntry, value))
                {
                    RefreshCommands();
                }
            }
        }

        public string Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
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

        public RelayCommand AddSupplyCommand { get; }

        public RelayCommand RemoveSupplyCommand { get; }

        public RelayCommand SaveCommand { get; }

        public RelayCommand ClearCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                Bars.Clear();
                Activities.Clear();
                Supplies.Clear();

                var bars = await _recoveryService.GetAvailableBarsAsync();
                var activities = await _recoveryService.GetActiveActivitiesAsync();
                var supplies = await _recoveryService.GetActiveSuppliesAsync();

                foreach (var bar in bars)
                    Bars.Add(bar);

                foreach (var activity in activities)
                    Activities.Add(activity);

                foreach (var supply in supplies)
                    Supplies.Add(supply);
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando datos: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task AddSupplyAsync()
        {
            if (SelectedSupply is null)
            {
                ShowError("Debe seleccionar un insumo.");
                return Task.CompletedTask;
            }

            if (!TryGetQuantity(out var parsedQuantity))
            {
                ShowError("Debe ingresar una cantidad válida mayor a cero.");
                return Task.CompletedTask;
            }

            var existing = UsedSupplies
                .FirstOrDefault(x => x.SupplyId == SelectedSupply.Id);

            if (existing is not null)
            {
                existing.Quantity += parsedQuantity;
                UsedSupplies.Remove(existing);
                UsedSupplies.Add(existing);
            }
            else
            {
                UsedSupplies.Add(new RecoverySupplyEntryItemViewModel
                {
                    Supply = SelectedSupply,
                    Quantity = parsedQuantity
                });
            }

            SelectedSupply = null;
            Quantity = string.Empty;

            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task RemoveSupplyAsync()
        {
            if (SelectedSupplyEntry is not null)
            {
                UsedSupplies.Remove(SelectedSupplyEntry);
                SelectedSupplyEntry = null;
            }

            RefreshCommands();

            return Task.CompletedTask;
        }

        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            if (!CanSave())
            {
                ShowError("Debe seleccionar barra, actividad y al menos un insumo.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var inputs = UsedSupplies
                    .Select(x => new RecoverySupplyInput
                    {
                        SupplyId = x.SupplyId,
                        Quantity = x.Quantity
                    })
                    .ToList();

                var saved = await _recoveryService.RegisterRecoveryAsync(
                    SelectedBar!.Id,
                    SelectedActivity!.Id,
                    Notes,
                    inputs);

                if (!saved)
                {
                    ShowError("No fue posible registrar la recuperación. Verifique permisos y datos.");
                    return;
                }

                ShowSuccess("Recuperación registrada correctamente. La barra queda pendiente de control de calidad.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error registrando recuperación: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task ClearAsync()
        {
            ClearForm();
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private bool CanAddSupply()
        {
            return !IsBusy
                   && SelectedSupply is not null
                   && TryGetQuantity(out _);
        }

        private bool CanRemoveSupply()
        {
            return !IsBusy && SelectedSupplyEntry is not null;
        }

        private bool CanSave()
        {
            return !IsBusy
                   && SelectedBar is not null
                   && SelectedActivity is not null
                   && UsedSupplies.Count > 0;
        }

        private bool TryGetQuantity(out double value)
        {
            return double.TryParse(
                       Quantity,
                       NumberStyles.Number,
                       CultureInfo.CurrentCulture,
                       out value)
                   && value > 0;
        }

        private void ClearForm()
        {
            SelectedBar = null;
            SelectedActivity = null;
            SelectedSupply = null;
            SelectedSupplyEntry = null;
            Quantity = string.Empty;
            Notes = string.Empty;
            UsedSupplies.Clear();
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
            AddSupplyCommand?.RaiseCanExecuteChanged();
            RemoveSupplyCommand?.RaiseCanExecuteChanged();
            SaveCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();
        }
    }
}