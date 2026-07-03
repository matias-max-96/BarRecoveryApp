using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("BarReturnReceipts")]
    public class BarReturnReceipt : EntityBase
    {
        [MaxLength(100)]
        public string? ReturnDocument { get; set; }

        public DateTime ReceivedAtUtc { get; set; } = DateTime.Now;

        [Indexed]
        [MaxLength(36)]
        public string ResponsibleUserId { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

        [MaxLength(100)]
        public string? RemoteId { get; set; }
    }
}