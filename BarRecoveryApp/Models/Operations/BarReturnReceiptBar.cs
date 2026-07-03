using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("BarReturnReceiptBars")]
    public class BarReturnReceiptBar : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string BarReturnReceiptId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string BarId { get; set; } = string.Empty;
    }
}