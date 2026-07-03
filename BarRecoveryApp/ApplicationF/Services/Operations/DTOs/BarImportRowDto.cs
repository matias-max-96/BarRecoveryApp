namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class BarImportRowDto
    {
        public int RowNumber { get; set; }

        public string BarNumber { get; set; } = string.Empty;

        public string PlantCode { get; set; } = string.Empty;

        public string BarTypeCode { get; set; } = string.Empty;
    }
}