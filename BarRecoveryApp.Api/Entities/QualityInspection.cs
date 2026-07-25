namespace BarRecoveryApp.Api.Entities
{
    public class QualityInspection : EntityBase
    {
        public string BarId { get; set; } = string.Empty;

        public string InspectorUserId { get; set; } = string.Empty;

        public DateTime InspectionAtUtc { get; set; }

        public int RecoveryCountAtInspection { get; set; }

        public bool CanBeRecovered { get; set; }

        public bool MustBeDisposed { get; set; }

        public bool IsApprovedForShipment { get; set; }

        public string? Notes { get; set; }

        public List<QualityInspectionAttributeValue> AttributeValues { get; set; } = new();
    }
}