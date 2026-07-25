namespace BarRecoveryApp.Api.Entities
{
    public class RecoveryWorkReport : EntityBase
    {
        public string UserId { get; set; } = string.Empty;

        public DateTime WorkDate { get; set; }

        public string? ShiftName { get; set; }

        public string? Notes { get; set; }

        public List<RecoveryWorkReportCategory> Categories { get; set; } = new();
    }
}