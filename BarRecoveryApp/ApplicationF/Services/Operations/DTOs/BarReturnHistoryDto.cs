namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class BarReturnHistoryDto
    {
        public DateTime ReceivedAt { get; set; }

        public string ReceivedAtText => ReceivedAt.ToString("dd-MM-yyyy HH:mm");

        public string? ReturnDocument { get; set; }

        public string? Notes { get; set; }

        public string ResponsibleName { get; set; } = string.Empty;
    }
}