namespace BarRecoveryApp.Api.Dtos
{
    public class PlantSyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }

    // Respuesta cuando un push es rechazado por LWW: el servidor tenía una
    // versión más nueva. La tablet debe adoptar ServerVersion localmente.
    public class SyncConflictDto
    {
        public string Message { get; set; } =
            "El servidor tiene una versión más reciente de este registro.";

        public PlantSyncDto ServerVersion { get; set; } = new();
    }
}
