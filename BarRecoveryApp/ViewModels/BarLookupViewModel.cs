using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ViewModels
{
    public class BarLookupViewModel : BaseViewModel
    {
        private readonly IBarLookupService _barLookupService;

        private const int DefaultMaxResults = 200;

        private string _searchText = string.Empty;
        private string _resultCountText = string.Empty;

        private string _errorMessage = string.Empty;
        private bool _hasError;

        public BarLookupViewModel(IBarLookupService barLookupService)
        {
            _barLookupService = barLookupService
                ?? throw new ArgumentNullException(nameof(barLookupService));

            Title = "Consultar barra";

            Bars = new ObservableCollection<BarLookupResultDto>();

            SearchCommand = new RelayCommand(SearchAsync);
            ClearCommand = new RelayCommand(ClearAsync);
        }

        public ObservableCollection<BarLookupResultDto> Bars { get; }

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

        public RelayCommand SearchCommand { get; }

        public RelayCommand ClearCommand { get; }

        private async Task SearchAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();

                Bars.Clear();

                var results = await _barLookupService.SearchBarsAsync(
                    SearchText,
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

        private Task ClearAsync()
        {
            SearchText = string.Empty;
            Bars.Clear();
            ResultCountText = string.Empty;
            ClearError();
            RefreshCommands();

            return Task.CompletedTask;
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
            SearchCommand?.RaiseCanExecuteChanged();
            ClearCommand?.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(ResultCountText));
        }
    }
}