using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Security
{
    [Table("Roles")]
    public class Role : CatalogBase
    {
        public bool IsSytemRole { get; set; } = false;
    }
}
