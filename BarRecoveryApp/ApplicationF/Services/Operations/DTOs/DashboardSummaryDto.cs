namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class DashboardSummaryDto
    {
        public int TotalBars { get; set; }

        public int ActiveBars { get; set; }

        public int InactiveBars { get; set; }

        public int DisposedBars { get; set; }

        public int ReadyToShipBars { get; set; }

        public int ShippedBars { get; set; }

        public int ReturnedBars { get; set; }

        public int CreatedBars { get; set; }

        public int ApprovedBars { get; set; }

        public int RejectedBars { get; set; }

        public int TotalRecoveryReports { get; set; }

        public int TotalShipments { get; set; }

        public int TotalReturns { get; set; }

        public List<PlantBarSummaryDto> BarsByPlant { get; set; } = new();

        public List<BarStatusSummaryDto> BarsByStatus { get; set; } = new();
    }
}