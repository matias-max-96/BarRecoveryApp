namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class ShipmentCreateResultDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string TechnicalReportFileName { get; set; } = string.Empty;

        public string TechnicalReportFilePath { get; set; } = string.Empty;

        public bool HasTechnicalReport
        {
            get
            {
                return !string.IsNullOrWhiteSpace(TechnicalReportFilePath);
            }
        }
    }
}
