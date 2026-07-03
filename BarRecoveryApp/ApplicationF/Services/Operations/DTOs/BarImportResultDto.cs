namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class BarImportResultDto
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalRowsRead { get; set; }
        public int CreatedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount => Errors.Count;
        public List<BarImportErrorDto> Errors { get; set; } = new();
        public bool Success => ErrorCount == 0;
        public string SummaryText =>
            $"Filas leídas: {TotalRowsRead} | Creadas: {CreatedCount} | Omitidas: {SkippedCount} | Errores: {ErrorCount}";
    }
}
