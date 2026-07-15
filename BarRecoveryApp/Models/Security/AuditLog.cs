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

        [MaxLength(150)]
        public string UserDisplayName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string RoleCode { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(100)]
        public string ActionCode { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string? EntityId { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? MetadataJson { get; set; }

        public string? OldValuesJson { get; set; }

        public string? NewValuesJson { get; set; }

        public DateTime OccurredAtUtc { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

        [MaxLength(100)]
        public string? RemoteId { get; set; }
    }
}