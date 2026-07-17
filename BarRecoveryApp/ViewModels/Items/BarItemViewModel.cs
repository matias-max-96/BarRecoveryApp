using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ViewModels.Items
{
    public class BarItemViewModel
    {
        public Bar Bar { get; set; } = default!;

        public string Id => Bar.Id;

        public string BarNumber => Bar.BarNumber;

        public string PlantName { get; set; } = string.Empty;

        public string BarTypeName { get; set; } = string.Empty;

        public string DisplayName => $"{BarNumber} - {PlantName} - {BarTypeName}";

        public int RecoveryCount => Bar.RecoveryCount;

        public bool IsDisposed => Bar.IsDisposed;

        public bool IsActive => Bar.IsActive;

        public BarStatus CurrentStatus => Bar.CurrentStatus;

        public string StatusName => CurrentStatus switch
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
        public string IsDisposedText
        {
            get
            {
                return IsDisposed ? "Sí" : "No";
            }
        }

        public string IsActiveText
        {
            get
            {
                return IsActive ? "Sí" : "No";
            }
        }
    }
}