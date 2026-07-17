using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("QualityInspectionAttributeValues")]
    public class QualityInspectionAttributeValue : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string QualityInspectionId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string BarId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string AttributeDefinitionId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string AttributeCode { get; set; } = string.Empty;

        [MaxLength(150)]
        public string AttributeName { get; set; } = string.Empty;

        public AttributeDataType DataType { get; set; }

        public bool WasMeasured { get; set; } = true;

        public string? ValueText { get; set; }

        public double? ValueNumber { get; set; }

        public DateTime? ValueDate { get; set; }

        public bool? ValueBool { get; set; }

        public bool? IsOutOfRange { get; set; }

        public double? MinValueAtInspection { get; set; }

        public double? MaxValueAtInspection { get; set; }

        [MaxLength(20)]
        public string? UnitAtInspection { get; set; }

        [MaxLength(200)]
        public string? ToleranceTextAtInspection { get; set; }
    }
}