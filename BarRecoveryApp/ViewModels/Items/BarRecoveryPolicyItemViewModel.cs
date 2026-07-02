using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ViewModels.Items
{
    public class BarRecoveryPolicyItemViewModel
    {
        public BarRecoveryPolicy Policy { get; set; } = default!;

        public string Id => Policy.Id;

        public string PlantId => Policy.PlantId;

        public string BarTypeId => Policy.BarTypeId;

        public string PlantName { get; set; } = string.Empty;

        public string BarTypeName { get; set; } = string.Empty;

        public int MaxRecoveries => Policy.MaxRecoveries;

        public bool IsActive => Policy.IsActive;
    }
}