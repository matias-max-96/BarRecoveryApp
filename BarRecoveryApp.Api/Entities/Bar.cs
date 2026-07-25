namespace BarRecoveryApp.Api.Entities
{
    public class Bar : EntityBase
    {
        public string BarNumber { get; set; } = string.Empty;

        public string PlantId { get; set; } = string.Empty;

        public string BarTypeId { get; set; } = string.Empty;

        // Espejo de BarStatus (Models.Enums) — se guarda como int, la
        // tablet es dueña del significado.
        public int CurrentStatus { get; set; }

        public int RecoveryCount { get; set; }

        public bool IsDisposed { get; set; }
    }
}