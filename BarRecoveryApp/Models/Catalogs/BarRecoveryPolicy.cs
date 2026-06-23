using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Catalogs
{
    [Table("BarRecoveryPolicies")]
    public class BarRecoveryPolicy : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string PlantId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string BarTypeId { get; set; } = string.Empty;

        public int MaxRecoveries { get; set; }
    }
}
