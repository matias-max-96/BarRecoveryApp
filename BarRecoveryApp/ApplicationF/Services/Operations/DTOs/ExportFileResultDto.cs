namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class ExportFileResultDto
    {
        public bool Success { get; set; }

        public string FilePath { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}