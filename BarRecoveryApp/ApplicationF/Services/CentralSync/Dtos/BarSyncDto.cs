namespace BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos
{
    public class BarSyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string BarNumber { get; set; } = string.Empty;

        public string PlantId { get; set; } = string.Empty;

        public string BarTypeId { get; set; } = string.Empty;

        public int CurrentStatus { get; set; }

        public int RecoveryCount { get; set; }

        public bool IsDisposed { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}