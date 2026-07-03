using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ViewModels
{
    public class BarHistoryViewModel : BaseViewModel
    {
        private readonly IBarHistoryService _service;

        private const int DefaultMaxResults = 200;

        private Plant? _selectedPlant;
        private BarType? _selectedBarType;
        private BarHistorySearchItemDto? _selectedBar;

        private string _searchText = string.Empty;
        private bool _includeInactive;

        private BarHistoryDetailDto? _history;

        private string _resultCountText = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError;

        public BarHistoryViewModel(IBarHistoryService service)
        {
            _service = service
                ?? throw new ArgumentNullException(nameof(service));

            Title = "Historial de barra";

            Plants = new ObservableCollection<Plant>();
            BarTypes = new ObservableCollection<BarType>();
            Bars = new ObservableCollection<BarHistorySearchItemDto>();

            LoadCommand = new RelayCommand(LoadAsync);
            SearchCommand = new RelayCommand(SearchAsync);
            LoadHistoryCommand = new RelayCommand(LoadHistoryAsync, CanLoadHistory);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public ObservableCollection<Plant> Plants { get; }

        public ObservableCollection<BarType> BarTypes { get; }

        public ObservableCollection<BarHistorySearchItemDto> Bars { get; }

        public Plant? SelectedPlant
        {
            get => _selectedPlant;
            set
            {
                if (SetProperty(ref _selectedPlant, value))
                {
                    ClearError();
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
                    ClearError();
                    RefreshCommands();
                }
            }
        }

        public BarHistorySearchItemDto? SelectedBar
        {
            get => _selectedBar;
            set
            {
                if (SetProperty(ref _selectedBar, value))
                {
                    History = null;
                    ClearError();
                    RefreshCommands();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ClearError();
                    RefreshCommands();
                }
            }
        }

        public bool IncludeInactive
        {
            get => _includeInactive;
            set
            {
                if (SetProperty(ref _includeInactive, value))
                {
                    ClearError();
                    RefreshCommands();
                }
            }
        }

        public BarHistoryDetailDto? History
        {
            get => _history;
            set
            {
                if (SetProperty(ref _history, value))
                {
                    OnPropertyChanged(nameof(HasHistory));
                }
            }
        }

        public bool HasHistory => History is not null;

        public string ResultCountText
        {
            get => _resultCountText;
            set => SetProperty(ref _resultCountText, value);
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

        public RelayCommand LoadCommand { get; }

        public RelayCommand SearchCommand { get; }

        public RelayCommand LoadHistoryCommand { get; }

        public RelayCommand ClearCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                Plants.Clear();
                BarTypes.Clear();
                Bars.Clear();

                var plants = await _service.GetActivePlantsAsync();
                var barTypes = await _service.GetActiveBarTypesAsync();

                foreach (var plant in plants)
                    Plants.Add(plant);

                foreach (var barType in barTypes)
                    BarTypes.Add(barType);

                ResultCountText = "Seleccione filtros o busque por número de barra.";
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando historial: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task SearchAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                Bars.Clear();
                SelectedBar = null;
                History = null;

                var results = await _service.SearchBarsAsync(
                    SelectedPlant?.Id,
                    SelectedBarType?.Id,
                    SearchText,
                    IncludeInactive,
                    DefaultMaxResults);

                foreach (var bar in results)
                    Bars.Add(bar);

                ResultCountText = Bars.Count == 0
                    ? "No se encontraron barras."
                    : $"Resultados: {Bars.Count} barra(s).";
            }
            catch (Exception ex)
            {
                ShowError($"Error buscando barras: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task LoadHistoryAsync()
        {
            if (SelectedBar is null)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                History = await _service.GetBarHistoryAsync(SelectedBar.BarId);

                if (History is null)
                {
                    ShowError("No fue posible cargar el historial de la barra.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error cargando historial de barra: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private Task ClearAsync()
        {
            SelectedPlant = null;
            SelectedBarType = null;
            SelectedBar = null;
            SearchText = string.Empty;
            IncludeInactive = false;
            Bars.Clear();
            History = null;
            ResultCountText = "Seleccione filtros o busque por número de barra.";
            ClearError();
            RefreshCommands();

            return Task.CompletedTask;
        }

        private bool CanLoadHistory()
        {
            return !IsBusy && SelectedBar is not null;
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        private void RefreshCommands()
        {
            LoadCommand?.RaiseCanExecuteChanged();
            SearchCommand?.RaiseCanExecuteChanged();
            LoadHistoryCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(ResultCountText));
            OnPropertyChanged(nameof(HasHistory));
        }
    }
}