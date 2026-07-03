namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class PlantBarSummaryDto
    {
        public string PlantName { get; set; } = string.Empty;

        public int TotalBars { get; set; }

        public int ReadyToShipBars { get; set; }

        public int ShippedBars { get; set; }

        public int ReturnedBars { get; set; }

        public int DisposedBars { get; set; }

        public string DisplayText =>
            $"{PlantName}: {TotalBars} barras";
    }
}