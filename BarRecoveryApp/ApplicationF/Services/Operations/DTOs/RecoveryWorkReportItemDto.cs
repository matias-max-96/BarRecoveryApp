namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class RecoveryWorkReportItemDto
    {
        public string Id { get; set; } = string.Empty;

        public DateTime WorkDate { get; set; }

        public string WorkDateText
        {
            get
            {
                return WorkDate.ToString("dd-MM-yyyy HH:mm");
            }
        }

        public string? ShiftName { get; set; }

        public string? Notes { get; set; }

        public List<RecoveryWorkCategoryDetailDto> Categories { get; set; } = new();

        public int TotalBarsWorkedCount
        {
            get
            {
                return Categories.Sum(x => x.BarsWorkedCount);
            }
        }

        public string CategoriesSummary
        {
            get
            {
                if (Categories.Count == 0)
                    return "Sin categorías";

                return string.Join(
                    " | ",
                    Categories.Select(x =>
                        $"{x.ExportLabel}: {x.BarsWorkedCount} barras"));
            }
        }

        public string ActivitiesSummary
        {
            get
            {
                if (Categories.Count == 0)
                    return "Sin actividades";

                return string.Join(
                    " | ",
                    Categories.Select(x =>
                        $"{x.ExportLabel}: {x.ActivitiesSummary}"));
            }
        }

        public string SuppliesSummary
        {
            get
            {
                if (Categories.Count == 0)
                    return "Sin insumos";

                return string.Join(
                    " | ",
                    Categories.Select(x =>
                        $"{x.ExportLabel}: {x.SuppliesSummary}"));
            }
        }
    }
}
