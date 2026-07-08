namespace BarRecoveryApp.ViewModels.Items
{
    public class RecoveryWorkSupplyItemViewModel
    {
        public string SupplyId { get; set; } = string.Empty;

        public string SupplyName { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public double Quantity { get; set; }

        public string DisplayText
        {
            get
            {
                return $"{SupplyName} - {Quantity:0.##} {Unit}";
            }
        }
    }
}