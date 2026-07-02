using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ViewModels.Items
{
    public class RecoverySupplyEntryItemViewModel
    {
        public Supply Supply { get; set; } = default!;

        public string SupplyId => Supply.Id;

        public string SupplyName => Supply.Name;

        public string Unit => Supply.Unit;

        public double Quantity { get; set; }

        public string DisplayText => $"{SupplyName} - {Quantity} {Unit}";
    }
}