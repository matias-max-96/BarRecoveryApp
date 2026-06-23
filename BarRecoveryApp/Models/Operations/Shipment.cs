using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Operations;

[Table("Shipments")]
public class Shipment : EntityBase
{
    [Indexed]
    [MaxLength(50)]
    public string TransferOrder { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CustomerReference { get; set; }

    public DateTime ShippedAtUtc { get; set; } = DateTime.UtcNow;

    [Indexed]
    [MaxLength(36)]
    public string ResponsibleUserId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

    [MaxLength(100)]
    public string? RemoteId { get; set; }
}