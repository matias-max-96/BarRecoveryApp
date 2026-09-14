using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class ShipmentBarTargetDto : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _weightKgText = string.Empty;

        public string BarId { get; set; } = string.Empty;

        public string BarNumber { get; set; } = string.Empty;

        public string PlantName { get; set; } = string.Empty;

        public string BarTypeName { get; set; } = string.Empty;

        public int RecoveryCount { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        // Solo lo usa BarReturnPage (pesaje obligatorio al recepcionar) —
        // en ShipmentPage queda sin usar, sin efecto.
        public string WeightKgText
        {
            get => _weightKgText;
            set
            {
                if (_weightKgText == value)
                    return;

                _weightKgText = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName =>
            $"{BarNumber} - {PlantName} - {BarTypeName}";

        public string DetailText =>
            $"Recuperaciones: {RecoveryCount}";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}