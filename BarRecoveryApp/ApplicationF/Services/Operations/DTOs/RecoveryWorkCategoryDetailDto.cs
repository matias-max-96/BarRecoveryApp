namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class RecoveryWorkCategoryDetailDto
    {
        public string ExportLabel { get; set; } = string.Empty;

        public int BarsWorkedCount { get; set; }

        public List<RecoveryWorkActivityDetailDto> Activities { get; set; } = new();

        public List<RecoveryWorkSupplyDetailDto> Supplies { get; set; } = new();

        public string DisplayText
        {
            get
            {
                return $"{ExportLabel} | Barras: {BarsWorkedCount}";
            }
        }

        public string ActivitiesSummary
        {
            get
            {
                if (Activities.Count == 0)
                    return "Sin actividades";

                return string.Join(
                    ", ",
                    Activities.Select(x =>
                        $"{x.ActivityName}: {x.HoursWorked:0.##} h"));
            }
        }

        public string SuppliesSummary
        {
            get
            {
                if (Supplies.Count == 0)
                    return "Sin insumos";

                return string.Join(
                    ", ",
                    Supplies.Select(x =>
                        $"{x.SupplyName}: {x.Quantity:0.##} {x.Unit}"));
            }
        }
    }
}