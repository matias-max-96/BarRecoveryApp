namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class BarStatusSummaryDto
    {
        public string StatusName { get; set; } = string.Empty;

        public int Count { get; set; }

        public string DisplayText =>
            $"{StatusName}: {Count}";
    }
}