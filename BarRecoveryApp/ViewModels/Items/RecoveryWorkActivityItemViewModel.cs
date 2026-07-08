namespace BarRecoveryApp.ViewModels.Items
{
    public class RecoveryWorkActivityItemViewModel
    {
        public string ActivityId { get; set; } = string.Empty;

        public string ActivityName { get; set; } = string.Empty;

        public double HoursWorked { get; set; }

        public string DisplayText
        {
            get
            {
                return $"{ActivityName} - {HoursWorked:0.##} h";
            }
        }
    }
}