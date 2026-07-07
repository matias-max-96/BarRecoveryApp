using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("RecoveryWorkReports")]
    public class RecoveryWorkReport : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string UserId { get; set; } = string.Empty;

        public DateTime WorkDate { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? ShiftName { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

        [MaxLength(100)]
        public string? RemoteId { get; set; }
    }
}
