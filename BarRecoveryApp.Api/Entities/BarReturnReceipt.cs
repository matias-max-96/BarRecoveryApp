namespace BarRecoveryApp.Api.Entities
{
    public class BarReturnReceipt : EntityBase
    {
        public string? ReturnDocument { get; set; }

        public DateTime ReceivedAtUtc { get; set; }

        public string ResponsibleUserId { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public List<BarReturnReceiptBar> Bars { get; set; } = new();
    }
}