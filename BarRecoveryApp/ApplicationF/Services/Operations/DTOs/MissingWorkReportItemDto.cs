namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class MissingWorkReportItemDto
    {
        public string UserId { get; set; } = string.Empty;

        public string OperatorName { get; set; } = string.Empty;

        public DateTime WorkDate { get; set; }

        public string WeekDayName
        {
            get
            {
                return WorkDate.DayOfWeek switch
                {
                    DayOfWeek.Monday => "lunes",
                    DayOfWeek.Tuesday => "martes",
                    DayOfWeek.Wednesday => "miércoles",
                    DayOfWeek.Thursday => "jueves",
                    DayOfWeek.Friday => "viernes",
                    DayOfWeek.Saturday => "sábado",
                    DayOfWeek.Sunday => "domingo",
                    _ => string.Empty
                };
            }
        }

        public string WorkDateText
        {
            get
            {
                return WorkDate.ToString("dd-MM-yyyy");
            }
        }

        public string DetailText
        {
            get
            {
                return $"{OperatorName} - {WeekDayName} {WorkDateText} - No registró trabajo";
            }
        }
    }
}