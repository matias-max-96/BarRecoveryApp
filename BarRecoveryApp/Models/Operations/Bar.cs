using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("Bars")]
    public class Bar : EntityBase
    {
        [MaxLength(100000)]
        public string BarNumber { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string PlantId { get; set; } = string.Empty;

        public string BarTypeId { get; set; } = string.Empty;

        public BarStatus CurrentStatus { get; set; } = BarStatus.Created;

        public int RecoveryCount { get; set; } = 0;

        public bool IsDisposed { get; set; } = false;
    }
}
