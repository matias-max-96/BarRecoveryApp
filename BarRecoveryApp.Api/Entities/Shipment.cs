namespace BarRecoveryApp.Api.Entities
{
    public class Shipment : EntityBase
    {
        public string TransferOrder { get; set; } = string.Empty;

        public string? CustomerReference { get; set; }

        public string? DispatchGuideNumber { get; set; }

        public DateTime ShippedAtUtc { get; set; }

        public string ResponsibleUserId { get; set; } = string.Empty;

        public List<ShipmentBar> Bars { get; set; } = new();
    }
}