using BarRecoveryApp.Models.Enums;

namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class BarInspectionTargetDto
    {
        public string BarId { get; set; } = string.Empty;

        public string BarNumber { get; set; } = string.Empty;

        public string PlantName { get; set; } = string.Empty;

        public string BarTypeName { get; set; } = string.Empty;

        public int CurrentRecoveryCount { get; set; }

        public int? MaxRecoveries { get; set; }

        public bool IsDisposed { get; set; }

        public BarStatus CurrentStatus { get; set; }

        public string DisplayName =>
            $"{BarNumber} {PlantName} - {BarTypeName}";

        public string RecoveryText =>
            MaxRecoveries.HasValue
                ? $"Recuperaciones: {CurrentRecoveryCount} / {MaxRecoveries.Value}"
                : $"Recuperaciones: {CurrentRecoveryCount}";

        public string StatusText =>
            IsDisposed
                ? "Estado: Dada de baja"
                : CurrentStatus switch
                {
                    BarStatus.Created => "Estado: Habilitada",
                    BarStatus.InRecovery => "Estado: En recuperación",
                    BarStatus.PendingQuality => "Estado: Pendiente calidad",
                    BarStatus.Approved => "Estado: Aprobada",
                    BarStatus.Rejected => "Estado: Rechazada",
                    BarStatus.ReadyToShip => "Estado: Aprobada para envío",
                    BarStatus.Shipped => "Estado: Enviada",
                    BarStatus.Disposed => "Estado: Dada de baja",
                    BarStatus.Returned => "Estado: Retornada / Disponible",
                    _ => "Estado: Desconocido"
                };

        public string PolicyText =>
            MaxRecoveries.HasValue
                ? $"Recuperaciones: {CurrentRecoveryCount} / {MaxRecoveries.Value}"
                : $"Recuperaciones: {CurrentRecoveryCount} / Sin política definida";

        public string IsDisposedText
        {
            get
            {
                return IsDisposed ? "Sí" : "No";
            }
        }
    }
}