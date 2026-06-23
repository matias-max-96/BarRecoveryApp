using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("RecoveryRecords")]
    public class RecoveryRecord : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string BarId {  get; set; } = string.Empty;

        [Indexed]
        [MaxLength (36)]
        public string UserId {  get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string ActivityId {  get; set; } = string.Empty;

        public DateTime PerformedAtUtc { get; set; } = DateTime.Now;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        public string DeviceId { get; set; }

        public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

        [MaxLength(100)]
        public string? RemoteId { get; set; }
    }
}
