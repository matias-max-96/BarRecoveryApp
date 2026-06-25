using System.Windows.Input;

namespace BarRecoveryApp.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public RelayCommand(
                    Func<Task> execute,
                    Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }


        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (_isExecuting)
                return false;

            return _canExecute?.Invoke() ?? true;
        }


        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

    }
}
