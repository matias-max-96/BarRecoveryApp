using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Catalogs;
using ActivityModel = BarRecoveryApp.Models.Catalogs.Activity;

namespace BarRecoveryApp.ViewModels
{
    public class ActivitiesViewModel : BaseViewModel
    {
        private readonly IActivityService _activityService;

        private ActivityModel? _selectedActivity;

        private string _code = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        private string _successMessage = string.Empty;
        private bool _hasSuccess;

        public ActivitiesViewModel(IActivityService activityService)
        {
            _activityService = activityService
                ?? throw new ArgumentNullException(nameof(activityService));

            Title = "Actividades";

            Activities = new ObservableCollection<ActivityModel>();

            LoadCommand = new RelayCommand(LoadAsync);
            SaveCommand = new RelayCommand(SaveAsync, CanSave);
            ToggleActiveCommand = new RelayCommand(ToggleActiveAsync, CanSelectActivity);
            NewCommand = new RelayCommand(NewAsync);
        }

        public ObservableCollection<ActivityModel> Activities { get; }

        public ActivityModel? SelectedActivity
        {
            get => _selectedActivity;
            set
            {
                if (SetProperty(ref _selectedActivity, value))
                {
                    LoadSelectedActivityToForm();
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
            SelectedActivity is null ? "Crear actividad" : "Actualizar actividad";

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

                Activities.Clear();

                var activities = await _activityService.GetActivitiesAsync();

                foreach (var activity in activities)
                    Activities.Add(activity);
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando actividades: {ex.Message}");
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
                ShowError("Debe ingresar código y nombre de la actividad.");
                return;
            }

            try
            {
                IsBusy = true;
                ClearMessages();

                var saved = await _activityService.SaveActivityAsync(
                    SelectedActivity?.Id,
                    Code,
                    Name,
                    Description);

                if (!saved)
                {
                    ShowError("No fue posible guardar la actividad. Verifique permisos o código duplicado.");
                    return;
                }

                ShowSuccess(SelectedActivity is null
                    ? "Actividad creada correctamente."
                    : "Actividad actualizada correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error guardando actividad: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedActivity is null)
                return;

            try
            {
                IsBusy = true;
                ClearMessages();

                var newState = !SelectedActivity.IsActive;

                var changed = await _activityService.SetActivityActiveStateAsync(
                    SelectedActivity.Id,
                    newState);

                if (!changed)
                {
                    ShowError("No fue posible cambiar el estado de la actividad.");
                    return;
                }

                ShowSuccess(newState
                    ? "Actividad activada correctamente."
                    : "Actividad desactivada correctamente.");

                ClearForm();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error cambiando estado de actividad: {ex.Message}");
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

        private bool CanSelectActivity()
        {
            return !IsBusy && SelectedActivity is not null;
        }

        private void LoadSelectedActivityToForm()
        {
            if (SelectedActivity is null)
            {
                Code = string.Empty;
                Name = string.Empty;
                Description = string.Empty;
            }
            else
            {
                Code = SelectedActivity.Code;
                Name = SelectedActivity.Name;
                Description = SelectedActivity.Description ?? string.Empty;
            }

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void ClearForm()
        {
            SelectedActivity = null;
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