namespace BarRecoveryApp.Api.Entities
{
    // Espejo de BarRecoveryApp.Models.Base.EntityBase, pero como entidad EF Core
    // para PostgreSQL. Mismo Id (string GUID) que las tablets, para que el
    // Id local de cada tablet sea directamente el Id remoto — sin necesidad
    // de mapeo de identificadores entre ambos lados.
    public abstract class EntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}
