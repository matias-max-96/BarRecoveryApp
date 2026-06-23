using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("BarAttributeValues")]
    public class BarAttributeValue : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string AttributeDefinitionId { get; set; } = string.Empty;

        public string? ValueText { get; set; }

        public double? ValueNumber { get; set; }

        public DateTime? ValueDate { get; set; }

        public bool? ValueBool { get; set; }

    }
}
