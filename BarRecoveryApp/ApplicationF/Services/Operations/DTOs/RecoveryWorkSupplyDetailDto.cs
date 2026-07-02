namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class RecoveryWorkSupplyDetailDto
    {
        public string SupplyId { get; set; } = string.Empty;

        public string SupplyName { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public double Quantity { get; set; }
    }
}