namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class ShipmentHistoryDto
    {
        public DateTime ShippedAt { get; set; }

        public string ShippedAtText => ShippedAt.ToString("dd-MM-yyyy HH:mm");

        public string TransferOrder { get; set; } = string.Empty;

        public string? CustomerReference { get; set; }

        public string ResponsibleName { get; set; } = string.Empty;
    }
}