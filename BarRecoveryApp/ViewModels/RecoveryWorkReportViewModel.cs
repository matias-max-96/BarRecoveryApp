using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.ViewModels.Items;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ViewModels
{
    public class RecoveryWorkReportViewModel : BaseViewModel
    {
        private readonly IRecoveryWorkReportService _service;

        private ActivityModel? _selectedActivity;
        private Supply? _selectedSupply;

        private string _workDate = DateTime.Now.ToString("dd-MM-yyyy");
        private string _shiftName = string.Empty;
        private string _barsWorkedCount = string.Empty;
        private string _activityHours = string.Empty;
        private string _supplyQuantity = string.Empty;
        private string _notes = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public RecoveryWorkReportViewModel(
            IRecoveryWorkReportService service)
        {
            _service = service
                ?? throw new ArgumentNullException(nameof(service));

            Title = "Registro de recuperación";

            Activities = new ObservableCollection<ActivityModel>();
            Supplies = new ObservableCollection<Supply>();

            WorkActivities = new ObservableCollection<RecoveryWorkActivityItemViewModel>();
            WorkSupplies = new ObservableCollection<RecoveryWorkSupplyItemViewModel>();

            LoadCommand = new RelayCommand(LoadAsync);
            AddActivityCommand = new RelayCommand(AddActivityAsync, CanAddActivity);
            AddSupplyCommand = new RelayCommand(AddSupplyAsync, CanAddSupply);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public ObservableCollection<ActivityModel> Activities { get; }

        public ObservableCollection<Supply> Supplies { get; }

        public ObservableCollection<RecoveryWorkActivityItemViewModel> WorkActivities { get; }

        public ObservableCollection<RecoveryWorkSupplyItemViewModel> WorkSupplies { get; }

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

        public string WorkDate
        {
            get => _workDate;
            set
            {
                if (SetProperty(ref _workDate, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string ShiftName
        {
            get => _shiftName;
            set
            {
                if (SetProperty(ref _shiftName, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string BarsWorkedCount
        {
            get => _barsWorkedCount;
            set
            {
                if (SetProperty(ref _barsWorkedCount, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string ActivityHours
        {
            get => _activityHours;
            set
            {
                if (SetProperty(ref _activityHours, value))
                {
                    ClearMessages();
                    RefreshCommands();
                }
            }
        }

        public string SupplyQuantity
        {
            get => _supplyQuantity;
            set
            {
                if (SetProperty(ref _supplyQuantity, value))
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

        public RelayCommand AddActivityCommand { get; }

        public RelayCommand AddSupplyCommand { get; }

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

                Activities.Clear();
                Supplies.Clear();

                var activities = await _service.GetActiveActivitiesAsync();
                var supplies = await _service.GetActiveSuppliesAsync();

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

        private Task AddActivityAsync()
        {
            if (SelectedActivity is null)
            {
                ShowError("Debe seleccionar una actividad.");
                return Task.CompletedTask;
            }

            if (!double.TryParse(ActivityHours, out var hours) || hours <= 0)
            {
                ShowError("Debe ingresar horas válidas.");
                return Task.CompletedTask;
            }

            WorkActivities.Add(new RecoveryWorkActivityItemViewModel
            {
                ActivityId = SelectedActivity.Id,
                ActivityName = SelectedActivity.Name,
                HoursWorked = hours
            });

            SelectedActivity = null;
            ActivityHours = string.Empty;

            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task AddSupplyAsync()
        {
            if (SelectedSupply is null)
            {
                ShowError("Debe seleccionar un insumo.");
                return Task.CompletedTask;
            }

            if (!double.TryParse(SupplyQuantity, out var quantity) || quantity <= 0)
            {
                ShowError("Debe ingresar una cantidad válida.");
                return Task.CompletedTask;
            }

            WorkSupplies.Add(new RecoveryWorkSupplyItemViewModel
            {
                SupplyId = SelectedSupply.Id,
                SupplyName = SelectedSupply.Name,
                Unit = SelectedSupply.Unit,
                Quantity = quantity
            });

            SelectedSupply = null;
            SupplyQuantity = string.Empty;

            RefreshCommands();

            return Task.CompletedTask;
        }

        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            if (!int.TryParse(BarsWorkedCount, out var barsCount) || barsCount <= 0)
            {
                ShowError("Debe ingresar una cantidad válida de barras trabajadas.");
                return;
            }

            if (WorkActivities.Count == 0)
            {
                ShowError("Debe agregar al menos una actividad.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var activityInputs = WorkActivities
                    .Select(x => new RecoveryWorkActivityInput
                    {
                        ActivityId = x.ActivityId,
                        HoursWorked = x.HoursWorked
                    })
                    .ToList();

                var supplyInputs = WorkSupplies
                    .Select(x => new RecoveryWorkSupplyInput
                    {
                        SupplyId = x.SupplyId,
                        Quantity = x.Quantity
                    })
                    .ToList();

                var saved = await _service.CreateReportAsync(
                    DateTime.Now,
                    ShiftName,
                    barsCount,
                    Notes,
                    activityInputs,
                    supplyInputs);

                if (!saved)
                {
                    ShowError("No fue posible guardar el registro. Verifique permisos o datos ingresados.");
                    return;
                }

                ShowSuccess("Registro de recuperación guardado correctamente.");

                ClearForm();
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando registro: {ex.Message}");
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

        private bool CanAddActivity()
        {
            return !IsBusy &&
                   SelectedActivity is not null &&
                   double.TryParse(ActivityHours, out var hours) &&
                   hours > 0;
        }

        private bool CanAddSupply()
        {
            return !IsBusy &&
                   SelectedSupply is not null &&
                   double.TryParse(SupplyQuantity, out var quantity) &&
                   quantity > 0;
        }

        private bool CanSave()
        {
            return !IsBusy &&
                   int.TryParse(BarsWorkedCount, out var bars) &&
                   bars > 0 &&
                   WorkActivities.Count > 0;
        }

        private void ClearForm()
        {
            WorkDate = DateTime.Now.ToString("dd-MM-yyyy");
            ShiftName = string.Empty;
            BarsWorkedCount = string.Empty;
            ActivityHours = string.Empty;
            SupplyQuantity = string.Empty;
            Notes = string.Empty;
            SelectedActivity = null;
            SelectedSupply = null;
            WorkActivities.Clear();
            WorkSupplies.Clear();
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
            AddActivityCommand?.RaiseCanExecuteChanged();
            AddSupplyCommand?.RaiseCanExecuteChanged();
            SaveCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();
        }
    }
}