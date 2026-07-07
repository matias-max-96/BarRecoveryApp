using BarRecoveryApp.Models.Enums;

namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class RecoveryWorkCategoryInput
    {
        public ProductionWorkType WorkType { get; set; }

        public string PlantId { get; set; } = string.Empty;

        public string BarTypeId { get; set; } = string.Empty;

        public int BarsWorkedCount { get; set; }

        public List<RecoveryWorkActivityInput> Activities { get; set; } = new();

        public List<RecoveryWorkSupplyInput> Supplies { get; set; } = new();
    }
}