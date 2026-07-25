namespace BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos
{
    public class BarReturnReceiptSyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string? ReturnDocument { get; set; }

        public DateTime ReceivedAtUtc { get; set; }

        public string ResponsibleUserId { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public List<string> BarIds { get; set; } = new();
    }
}