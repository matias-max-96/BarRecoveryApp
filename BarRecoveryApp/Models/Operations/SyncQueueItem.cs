using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("SyncQueue")]
    public class SyncQueueItem : EntityBase
    {
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(36)]
        public string EntityLocalId { get; set; } = string.Empty;

        public SyncOperationType OperationType { get; set; }

        public string PayloadJson { get; set; } = string.Empty;

        public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

        public int Retries { get; set; } = 0;

        public DateTime? LastAttemptUtc { get; set; }

        public string? ErrorMessage { get; set; }


    }
}