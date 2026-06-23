using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Security
{
    [Table("Permissions")]
    public class Permission : CatalogBase
    {
        [MaxLength(100)]
        public string Module { get; set; } = string.Empty;
    }
}
