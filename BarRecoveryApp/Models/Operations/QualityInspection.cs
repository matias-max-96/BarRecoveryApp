using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("QualityInspections")]
    public class QualityInspection : EntityBase
    {

        [Indexed]
        [MaxLength(36)]
        public string InspectorUserId { get; set; } = string.Empty;

        public DateTime InspectionAtUtc { get; set; } = DateTime.Now;

        public int RecoveryCountAtInspection { get; set; }

        public bool CanBeRecovered { get; set; }

        public bool MustBeDisposed { get; set; }

        public bool IsApprovedForShipment { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

        [MaxLength(100)]
        public string? RemoteId { get; set; }

    }
}
