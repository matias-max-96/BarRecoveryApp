namespace BarRecoveryApp.Api.Entities
{
    public abstract class CatalogEntityBase : EntityBase
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
