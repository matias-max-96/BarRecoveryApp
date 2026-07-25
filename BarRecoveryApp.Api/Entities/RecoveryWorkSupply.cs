namespace BarRecoveryApp.Api.Entities
{
    public class RecoveryWorkSupply : EntityBase
    {
        public string RecoveryWorkReportCategoryId { get; set; } = string.Empty;

        public string SupplyId { get; set; } = string.Empty;

        public double Quantity { get; set; }
    }
}