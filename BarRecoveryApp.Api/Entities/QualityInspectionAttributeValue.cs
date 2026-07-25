namespace BarRecoveryApp.Api.Entities
{
    public class QualityInspectionAttributeValue : EntityBase
    {
        public string QualityInspectionId { get; set; } = string.Empty;

        public string BarId { get; set; } = string.Empty;

        public string AttributeDefinitionId { get; set; } = string.Empty;

        public string AttributeCode { get; set; } = string.Empty;

        public string AttributeName { get; set; } = string.Empty;

        // Espejo de AttributeDataType (Models.Enums) — se guarda como int,
        // la tablet es dueña del significado.
        public int DataType { get; set; }

        public bool WasMeasured { get; set; }

        public string? ValueText { get; set; }

        public double? ValueNumber { get; set; }

        public DateTime? ValueDate { get; set; }

        public bool? ValueBool { get; set; }

        public bool? IsOutOfRange { get; set; }

        public double? MinValueAtInspection { get; set; }

        public double? MaxValueAtInspection { get; set; }

        public string? UnitAtInspection { get; set; }

        public string? ToleranceTextAtInspection { get; set; }
    }
}