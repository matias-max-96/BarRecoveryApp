namespace BarRecoveryApp.Api.Dtos
{
    public class QualityInspectionSyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string BarId { get; set; } = string.Empty;

        public string InspectorUserId { get; set; } = string.Empty;

        public DateTime InspectionAtUtc { get; set; }

        public int RecoveryCountAtInspection { get; set; }

        public bool CanBeRecovered { get; set; }

        public bool MustBeDisposed { get; set; }

        public bool IsApprovedForShipment { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public List<QualityInspectionAttributeValueSyncDto> AttributeValues { get; set; } = new();
    }

    public class QualityInspectionAttributeValueSyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string BarId { get; set; } = string.Empty;

        public string AttributeDefinitionId { get; set; } = string.Empty;

        public string AttributeCode { get; set; } = string.Empty;

        public string AttributeName { get; set; } = string.Empty;

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