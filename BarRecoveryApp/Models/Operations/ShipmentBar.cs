using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("ShipmentBar")]
    public class ShipmentBar : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string ShipmentId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string BarId { get; set; } = string.Empty;
    }
}
