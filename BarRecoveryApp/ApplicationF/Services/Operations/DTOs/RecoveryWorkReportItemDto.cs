namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class RecoveryWorkReportItemDto
    {
        public string Id { get; set; } = string.Empty;

        public DateTime WorkDate { get; set; }

        public string WorkDateText => WorkDate.ToString("dd-MM-yyyy HH:mm");

        public string? ShiftName { get; set; }

        public int BarsWorkedCount { get; set; }

        public string? Notes { get; set; }

        public List<RecoveryWorkActivityDetailDto> Activities { get; set; } = new();

        public List<RecoveryWorkSupplyDetailDto> Supplies { get; set; } = new();

        public string ActivitiesSummary =>
            Activities.Count == 0
                ? "Sin actividades"
                : string.Join(", ", Activities.Select(x => $"{x.ActivityName}: {x.HoursWorked:0.##} h"));

        public string SuppliesSummary =>
            Supplies.Count == 0
                ? "Sin insumos"
                : string.Join(", ", Supplies.Select(x => $"{x.SupplyName}: {x.Quantity:0.##} {x.Unit}"));
    }
}