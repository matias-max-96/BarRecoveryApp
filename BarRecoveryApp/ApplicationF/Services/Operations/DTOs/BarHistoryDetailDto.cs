namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class BarHistoryDetailDto
    {
        public string BarId { get; set; } = string.Empty;

        public string BarNumber { get; set; } = string.Empty;

        public string PlantName { get; set; } = string.Empty;

        public string BarTypeName { get; set; } = string.Empty;

        public int RecoveryCount { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public bool IsDisposed { get; set; }

        public List<QualityInspectionHistoryDto> Inspections { get; set; } = new();

        public List<ShipmentHistoryDto> Shipments { get; set; } = new();

        public List<BarReturnHistoryDto> Returns { get; set; } = new();

        public string HeaderText =>
            $"{BarNumber} - {PlantName} - {BarTypeName}";

        public string SummaryText =>
            $"Recuperaciones: {RecoveryCount} | Estado: {StatusText}";

        public string IsDisposedText
        {
            get
            {
                return IsDisposed ? "Sí" : "No";
            }
        }
    }
}