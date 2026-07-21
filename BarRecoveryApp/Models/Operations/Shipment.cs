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

    // Número de guía de despacho, tal como aparece en el documento físico
    // entregado por Saalasti. Texto libre a propósito el formato varía y 
    // no se puede normalizar de forma confiable a un correlativo numérico. 
    [MaxLength(100)]
    public string? DispatchGuideNumber { get; set; }

    public DateTime ShippedAtUtc { get; set; } = DateTime.Now;

    [Indexed]
    [MaxLength(36)]
    public string ResponsibleUserId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

    [MaxLength(100)]
    public string? RemoteId { get; set; }
}