using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Catalogs
{
    [Table("Supplies")]
    public class Supply : CatalogBase
    {
        [MaxLength(20)]
        public string Unit { get; set; } = string.Empty;
    }
}
