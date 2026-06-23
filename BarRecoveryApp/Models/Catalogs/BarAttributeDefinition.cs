using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Catalogs
{
    [Table("BarAttributeDefinitions")]
    public class BarAttributeDefinition : EntityBase
    {
        [Indexed(Unique = true)]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public AttributeDataType DataType { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        public bool IsRequired { get; set; } = false;

        [MaxLength (36)]
        public string? AppliesToBarTypeId { get; set;  }

        [MaxLength(36)]
        public string? AppliesToPlantId { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [MaxLength(36)]
        public string CreatedByUserId { get; set; } = string.Empty;

    }
}
