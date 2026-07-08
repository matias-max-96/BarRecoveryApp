using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.ViewModels.Items;
using BarRecoveryApp.ViewModels.Options;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ViewModels
{
    public class RecoveryWorkReportViewModel : BaseViewModel
    {
        private readonly IRecoveryWorkReportService _service;

        private ProductionWorkTypeOption? _selectedWorkType;
        private Plant? _selectedPlant;
        private BarType? _selectedBarType;
        private ActivityModel? _selectedActivity;
        private Supply? _selectedSupply;

        private RecoveryWorkActivityItemViewModel? _selectedActivityItem;
        private RecoveryWorkSupplyItemViewModel? _selectedSupplyItem;
        private RecoveryWorkCategoryItemViewModel? _selectedCategoryItem;

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

            WorkTypes = new ObservableCollection<ProductionWorkTypeOption>
            {
                new ProductionWorkTypeOption
                {
                    Value = ProductionWorkType.Recovery,
                    Name = "Recuperación"
                },
                new ProductionWorkTypeOption
                {
                    Value = ProductionWorkType.Fabrication,
                    Name = "Fabricación"
                }
            };

            Plants = new ObservableCollection<Plant>();
            BarTypes = new ObservableCollection<BarType>();
            Activities = new ObservableCollection<ActivityModel>();
            Supplies = new ObservableCollection<Supply>();

            CurrentActivities = new ObservableCollection<RecoveryWorkActivityItemViewModel>();
            CurrentSupplies = new ObservableCollection<RecoveryWorkSupplyItemViewModel>();
            Categories = new ObservableCollection<RecoveryWorkCategoryItemViewModel>();

            SelectedWorkType = WorkTypes.FirstOrDefault();

            LoadCommand = new RelayCommand(LoadAsync);
            AddActivityCommand = new RelayCommand(AddActivityAsync, CanAddActivity);
            RemoveActivityCommand = new RelayCommand(RemoveActivityAsync, CanRemoveActivity);
            AddSupplyCommand = new RelayCommand(AddSupplyAsync, CanAddSupply);
            RemoveSupplyCommand = new RelayCommand(RemoveSupplyAsync, CanRemoveSupply);
            AddCategoryCommand = new RelayCommand(AddCategoryAsync, CanAddCategory);
            RemoveCategoryCommand = new RelayCommand(RemoveCategoryAsync, CanRemoveCategory);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public ObservableCollection<ProductionWorkTypeOption> WorkTypes { get; }

        public ObservableCollection<Plant> Plants { get; }

        public ObservableCollection<BarType> BarTypes { get; }

        public ObservableCollection<ActivityModel> Activities { get; }

        public ObservableCollection<Supply> Supplies { get; }

        public ObservableCollection<RecoveryWorkActivityItemViewModel> CurrentActivities { get; }

        public ObservableCollection<RecoveryWorkSupplyItemViewModel> CurrentSupplies { get; }

        public ObservableCollection<RecoveryWorkCategoryItemViewModel> Categories { get; }

        public ProductionWorkTypeOption? SelectedWorkType
        {
            get => _selectedWorkType;
            set
            {
                if (SetProperty(ref _selectedWorkType, value))
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

        public RecoveryWorkActivityItemViewModel? SelectedActivityItem
        {
            get => _selectedActivityItem;
            set
            {
                if (SetProperty(ref _selectedActivityItem, value))
                {
                    RefreshCommands();
                }
            }
        }

        public RecoveryWorkSupplyItemViewModel? SelectedSupplyItem
        {
            get => _selectedSupplyItem;
            set
            {
                if (SetProperty(ref _selectedSupplyItem, value))
                {
                    RefreshCommands();
                }
            }
        }

        public RecoveryWorkCategoryItemViewModel? SelectedCategoryItem
        {
            get => _selectedCategoryItem;
            set
            {
                if (SetProperty(ref _selectedCategoryItem, value))
                {
                    RefreshCommands();
                }
            }
        }

        public string WorkDate
        {
            get => _workDate;
            set => SetProperty(ref _workDate, value);
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

        public RelayCommand RemoveActivityCommand { get; }

        public RelayCommand AddSupplyCommand { get; }

        public RelayCommand RemoveSupplyCommand { get; }

        public RelayCommand AddCategoryCommand { get; }

        public RelayCommand RemoveCategoryCommand { get; }

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

                Plants.Clear();
                BarTypes.Clear();
                Activities.Clear();
                Supplies.Clear();

                var plants = await _service.GetActivePlantsAsync();
                var barTypes = await _service.GetActiveBarTypesAsync();
                var activities = await _service.GetActiveActivitiesAsync();
                var supplies = await _service.GetActiveSuppliesAsync();

                foreach (var plant in plants)
                    Plants.Add(plant);

                foreach (var barType in barTypes)
                    BarTypes.Add(barType);

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

            var existing = CurrentActivities
                .FirstOrDefault(x => x.ActivityId == SelectedActivity.Id);

            if (existing is not null)
            {
                existing.HoursWorked += hours;
                CurrentActivities.Remove(existing);
                CurrentActivities.Add(existing);
            }
            else
            {
                CurrentActivities.Add(new RecoveryWorkActivityItemViewModel
                {
                    ActivityId = SelectedActivity.Id,
                    ActivityName = SelectedActivity.Name,
                    HoursWorked = hours
                });
            }

            SelectedActivity = null;
            ActivityHours = string.Empty;

            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task RemoveActivityAsync()
        {
            if (SelectedActivityItem is not null)
            {
                CurrentActivities.Remove(SelectedActivityItem);
                SelectedActivityItem = null;
            }

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

            var existing = CurrentSupplies
                .FirstOrDefault(x => x.SupplyId == SelectedSupply.Id);

            if (existing is not null)
            {
                existing.Quantity += quantity;
                CurrentSupplies.Remove(existing);
                CurrentSupplies.Add(existing);
            }
            else
            {
                CurrentSupplies.Add(new RecoveryWorkSupplyItemViewModel
                {
                    SupplyId = SelectedSupply.Id,
                    SupplyName = SelectedSupply.Name,
                    Unit = SelectedSupply.Unit,
                    Quantity = quantity
                });
            }

            SelectedSupply = null;
            SupplyQuantity = string.Empty;

            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task RemoveSupplyAsync()
        {
            if (SelectedSupplyItem is not null)
            {
                CurrentSupplies.Remove(SelectedSupplyItem);
                SelectedSupplyItem = null;
            }

            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task AddCategoryAsync()
        {
            if (!CanAddCategory())
            {
                ShowError("Debe seleccionar tipo de trabajo, planta, tipo de barra, cantidad de barras y al menos una actividad.");
                return Task.CompletedTask;
            }

            var barsCount = int.Parse(BarsWorkedCount);

            var category = new RecoveryWorkCategoryItemViewModel
            {
                WorkType = SelectedWorkType!.Value,
                WorkTypeName = SelectedWorkType.Name,
                PlantId = SelectedPlant!.Id,
                PlantName = SelectedPlant.Name,
                BarTypeId = SelectedBarType!.Id,
                BarTypeName = SelectedBarType.Name,
                BarsWorkedCount = barsCount
            };

            foreach (var activity in CurrentActivities)
            {
                category.Activities.Add(new RecoveryWorkActivityItemViewModel
                {
                    ActivityId = activity.ActivityId,
                    ActivityName = activity.ActivityName,
                    HoursWorked = activity.HoursWorked
                });
            }

            foreach (var supply in CurrentSupplies)
            {
                category.Supplies.Add(new RecoveryWorkSupplyItemViewModel
                {
                    SupplyId = supply.SupplyId,
                    SupplyName = supply.SupplyName,
                    Unit = supply.Unit,
                    Quantity = supply.Quantity
                });
            }

            Categories.Add(category);

            ClearCurrentCategoryForm();
            ClearMessages();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private Task RemoveCategoryAsync()
        {
            if (SelectedCategoryItem is not null)
            {
                Categories.Remove(SelectedCategoryItem);
                SelectedCategoryItem = null;
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
                ShowError("Debe agregar al menos una categoría antes de guardar.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var inputs = Categories
                    .Select(category => new RecoveryWorkCategoryInput
                    {
                        WorkType = category.WorkType,
                        PlantId = category.PlantId,
                        BarTypeId = category.BarTypeId,
                        BarsWorkedCount = category.BarsWorkedCount,
                        Activities = category.Activities
                            .Select(activity => new RecoveryWorkActivityInput
                            {
                                ActivityId = activity.ActivityId,
                                HoursWorked = activity.HoursWorked
                            })
                            .ToList(),
                        Supplies = category.Supplies
                            .Select(supply => new RecoveryWorkSupplyInput
                            {
                                SupplyId = supply.SupplyId,
                                Quantity = supply.Quantity
                            })
                            .ToList()
                    })
                    .ToList();

                var saved = await _service.CreateReportAsync(
                    DateTime.Now,
                    ShiftName,
                    Notes,
                    inputs);

                if (!saved)
                {
                    ShowError("No fue posible guardar el registro. Verifique permisos o datos ingresados.");
                    return;
                }

                ClearForm();

                ShowSuccess("Registro de recuperación guardado correctamente.");
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

        private bool CanRemoveActivity()
        {
            return !IsBusy && SelectedActivityItem is not null;
        }

        private bool CanAddSupply()
        {
            return !IsBusy &&
                   SelectedSupply is not null &&
                   double.TryParse(SupplyQuantity, out var quantity) &&
                   quantity > 0;
        }

        private bool CanRemoveSupply()
        {
            return !IsBusy && SelectedSupplyItem is not null;
        }

        private bool CanAddCategory()
        {
            return !IsBusy &&
                   SelectedWorkType is not null &&
                   SelectedPlant is not null &&
                   SelectedBarType is not null &&
                   int.TryParse(BarsWorkedCount, out var barsCount) &&
                   barsCount > 0 &&
                   CurrentActivities.Count > 0;
        }

        private bool CanRemoveCategory()
        {
            return !IsBusy && SelectedCategoryItem is not null;
        }

        private bool CanSave()
        {
            return !IsBusy && Categories.Count > 0;
        }

        private void ClearCurrentCategoryForm()
        {
            SelectedWorkType = WorkTypes.FirstOrDefault();
            SelectedPlant = null;
            SelectedBarType = null;
            BarsWorkedCount = string.Empty;

            SelectedActivity = null;
            ActivityHours = string.Empty;
            SelectedActivityItem = null;
            CurrentActivities.Clear();

            SelectedSupply = null;
            SupplyQuantity = string.Empty;
            SelectedSupplyItem = null;
            CurrentSupplies.Clear();
        }

        private void ClearForm()
        {
            WorkDate = DateTime.Now.ToString("dd-MM-yyyy");
            ShiftName = string.Empty;
            Notes = string.Empty;

            ClearCurrentCategoryForm();

            SelectedCategoryItem = null;
            Categories.Clear();
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
            RemoveActivityCommand?.RaiseCanExecuteChanged();
            AddSupplyCommand?.RaiseCanExecuteChanged();
            RemoveSupplyCommand?.RaiseCanExecuteChanged();
            AddCategoryCommand?.RaiseCanExecuteChanged();
            RemoveCategoryCommand?.RaiseCanExecuteChanged();
            SaveCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();
        }
    }
}