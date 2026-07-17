namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class QualityInspectionAttributeValueInputDto
    {
        public string AttributeDefinitionId { get; set; } = string.Empty;

        public bool WasMeasured { get; set; } = true;

        public string? ValueText { get; set; }

        public double? ValueNumber { get; set; }

        public DateTime? ValueDate { get; set; }

        public bool? ValueBool { get; set; }
    }
}