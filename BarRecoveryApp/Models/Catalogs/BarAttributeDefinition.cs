using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Catalogs
{
    [Table("BarAttributeDefinitions")]
    public class BarAttributeDefinition : EntityBase
    {
        [Indexed]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public AttributeDataType DataType { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        public bool IsRequired { get; set; } = false;

        public bool HasRangeValidation { get; set; } = false;

        public double? MinValue { get; set; }

        public double? MaxValue { get; set; }

        [MaxLength(200)]
        public string? ToleranceText { get; set; }

        [Indexed]
        [MaxLength(36)]
        public string? AppliesToBarTypeId { get; set; }

        [Indexed]
        [MaxLength(36)]
        public string? AppliesToPlantId { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [MaxLength(36)]
        public string CreatedByUserId { get; set; } = string.Empty;
    }
}