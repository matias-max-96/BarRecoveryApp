namespace BarRecoveryApp.Api.Dtos
{
    public class RecoveryWorkReportSyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public DateTime WorkDate { get; set; }

        public string? ShiftName { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public List<RecoveryWorkReportCategorySyncDto> Categories { get; set; } = new();
    }

    public class RecoveryWorkReportCategorySyncDto
    {
        public string Id { get; set; } = string.Empty;

        public int WorkType { get; set; }

        public string PlantId { get; set; } = string.Empty;

        public string BarTypeId { get; set; } = string.Empty;

        public int BarsWorkedCount { get; set; }

        public string ExportLabel { get; set; } = string.Empty;

        public List<RecoveryWorkActivitySyncDto> Activities { get; set; } = new();

        public List<RecoveryWorkSupplySyncDto> Supplies { get; set; } = new();
    }

    public class RecoveryWorkActivitySyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string ActivityId { get; set; } = string.Empty;

        public double HoursWorked { get; set; }
    }

    public class RecoveryWorkSupplySyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string SupplyId { get; set; } = string.Empty;

        public double Quantity { get; set; }
    }
}