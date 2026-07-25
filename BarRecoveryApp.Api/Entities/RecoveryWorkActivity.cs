namespace BarRecoveryApp.Api.Entities
{
    public class RecoveryWorkActivity : EntityBase
    {
        public string RecoveryWorkReportCategoryId { get; set; } = string.Empty;

        public string ActivityId { get; set; } = string.Empty;

        public double HoursWorked { get; set; }
    }
}