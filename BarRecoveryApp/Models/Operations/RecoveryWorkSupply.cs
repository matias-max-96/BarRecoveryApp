using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("RecoveryWorkSupplies")]
    public class RecoveryWorkSupply : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string RecoveryWorkReportId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string SupplyId { get; set; } = string.Empty;

        public double Quantity { get; set; }
    }
}