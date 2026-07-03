namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class QualityInspectionHistoryDto
    {
        public DateTime InspectionAt { get; set; }

        public string InspectionAtText => InspectionAt.ToString("dd-MM-yyyy HH:mm");

        public string InspectorName { get; set; } = string.Empty;

        public int RecoveryCountAtInspection { get; set; }

        public bool CanBeRecovered { get; set; }

        public bool MustBeDisposed { get; set; }

        public bool IsApprovedForShipment { get; set; }

        public string? Notes { get; set; }

        public string ResultText
        {
            get
            {
                if (MustBeDisposed)
                    return "Dada de baja";

                if (IsApprovedForShipment)
                    return "Aprobada para envío";

                if (CanBeRecovered)
                    return "Puede recuperarse nuevamente";

                return "Rechazada / No recuperable";
            }
        }
    }
}