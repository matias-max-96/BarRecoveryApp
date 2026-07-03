using BarRecoveryApp.Models.Enums;

namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class BarHistorySearchItemDto
    {
        public string BarId { get; set; } = string.Empty;

        public string BarNumber { get; set; } = string.Empty;

        public string PlantName { get; set; } = string.Empty;

        public string BarTypeName { get; set; } = string.Empty;

        public int RecoveryCount { get; set; }

        public bool IsDisposed { get; set; }

        public bool IsActive { get; set; }

        public BarStatus CurrentStatus { get; set; }

        public string DisplayName =>
            $"{BarNumber} - {PlantName} - {BarTypeName}";

        public string DetailText =>
            $"Recuperaciones: {RecoveryCount} | Estado: {StatusText}";

        public string StatusText =>
            IsDisposed
                ? "Dada de baja"
                : CurrentStatus switch
                {
                    BarStatus.Created => "Creada",
                    BarStatus.InRecovery => "En recuperación",
                    BarStatus.PendingQuality => "Pendiente calidad",
                    BarStatus.Approved => "Aprobada",
                    BarStatus.Rejected => "Rechazada",
                    BarStatus.ReadyToShip => "Lista para envío",
                    BarStatus.Shipped => "Enviada",
                    BarStatus.Disposed => "Dada de baja",
                    BarStatus.Returned => "Retornada / Disponible",
                    _ => "Desconocido"
                };
    }
}