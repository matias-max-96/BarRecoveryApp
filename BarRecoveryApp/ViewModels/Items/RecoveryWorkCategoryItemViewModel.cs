using System.Collections.ObjectModel;
using BarRecoveryApp.Models.Enums;

namespace BarRecoveryApp.ViewModels.Items
{
    public class RecoveryWorkCategoryItemViewModel
    {
        public ProductionWorkType WorkType { get; set; }

        public string WorkTypeName { get; set; } = string.Empty;

        public string PlantId { get; set; } = string.Empty;

        public string PlantName { get; set; } = string.Empty;

        public string BarTypeId { get; set; } = string.Empty;

        public string BarTypeName { get; set; } = string.Empty;

        public int BarsWorkedCount { get; set; }

        public ObservableCollection<RecoveryWorkActivityItemViewModel> Activities { get; set; } = new();

        public ObservableCollection<RecoveryWorkSupplyItemViewModel> Supplies { get; set; } = new();

        public string DisplayText
        {
            get
            {
                return $"{WorkTypeName} {PlantName} - {BarTypeName} | Barras: {BarsWorkedCount}";
            }
        }

        public string ActivitiesSummary
        {
            get
            {
                if (Activities.Count == 0)
                    return "Sin actividades";

                return string.Join(", ", Activities.Select(x => x.DisplayText));
            }
        }

        public string SuppliesSummary
        {
            get
            {
                if (Supplies.Count == 0)
                    return "Sin insumos";

                return string.Join(", ", Supplies.Select(x => x.DisplayText));
            }
        }
    }
}