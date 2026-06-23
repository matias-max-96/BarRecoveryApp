using BarRecoveryApp.Models.Base;
using SQLite

namespace BarRecoveryApp.Models.Operations
{
    [Table("RecoveryRecordSupplies")]
    public class RecoveryRecordSupply : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string RecoveryRecordId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string SupplyId {  get; set; } = string.Empty;

        public double Quantity { get; set; }
    }
}
