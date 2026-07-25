namespace BarRecoveryApp.Api.Dtos
{
    public class ShipmentSyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string TransferOrder { get; set; } = string.Empty;

        public string? CustomerReference { get; set; }

        public string? DispatchGuideNumber { get; set; }

        public DateTime ShippedAtUtc { get; set; }

        public string ResponsibleUserId { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public List<string> BarIds { get; set; } = new();
    }
}