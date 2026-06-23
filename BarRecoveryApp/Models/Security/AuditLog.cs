using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Security
{
    [Table("AuditLogs")]
    public class AuditLog : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ActionCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(36)]
        public string EntityId { get; set; } = string.Empty;

        public string? OldValuesJson { get; set; }

        public string? NewValuesJson { get; set; }

        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

    }
}
