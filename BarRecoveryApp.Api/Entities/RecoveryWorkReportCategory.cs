namespace BarRecoveryApp.Api.Entities
{
    public class RecoveryWorkReportCategory : EntityBase
    {
        public string RecoveryWorkReportId { get; set; } = string.Empty;

        // Espejo de ProductionWorkType (Models.Enums) — se guarda como int
        // en vez de duplicar el enum acá; el cliente es dueño del significado.
        public int WorkType { get; set; }

        public string PlantId { get; set; } = string.Empty;

        public string BarTypeId { get; set; } = string.Empty;

        public int BarsWorkedCount { get; set; }

        public string ExportLabel { get; set; } = string.Empty;

        public List<RecoveryWorkActivity> Activities { get; set; } = new();

        public List<RecoveryWorkSupply> Supplies { get; set; } = new();
    }
}