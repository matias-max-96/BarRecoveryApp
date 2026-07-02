namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class RecoveryWorkActivityInput
    {
        public string ActivityId { get; set; } = string.Empty;

        public double HoursWorked { get; set; }
    }
}