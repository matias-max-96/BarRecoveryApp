namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class RecoveryWorkActivityDetailDto
    {
        public string ActivityId { get; set; } = string.Empty;

        public string ActivityName { get; set; } = string.Empty;

        public double HoursWorked { get; set; }
    }
}