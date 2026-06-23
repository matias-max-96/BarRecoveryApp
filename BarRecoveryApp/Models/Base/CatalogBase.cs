using SQLite;

namespace BarRecoveryApp.Models.Base
{
    public abstract class CatalogBase : EntityBase
    {
        [Indexed(Unique = true)]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength (500)]
        public string? Description { get; set; } = string.Empty;
    }
}
