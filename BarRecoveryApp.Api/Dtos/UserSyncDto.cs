namespace BarRecoveryApp.Api.Dtos
{
    public class UserSyncDto
    {
        public string Id { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string RoleCode { get; set; } = string.Empty;

        public string PinHash { get; set; } = string.Empty;

        public string PinSalt { get; set; } = string.Empty;

        public bool IsPinEnabled { get; set; }

        public bool MustChangePin { get; set; }

        public string CreatedByUserId { get; set; } = string.Empty;

        public string? UpdatedByUserId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }

    public class UserSyncConflictDto
    {
        public string Message { get; set; } =
            "El servidor tiene una versión más reciente de este registro.";

        public UserSyncDto ServerVersion { get; set; } = new();
    }

    public class UserActiveStatusDto
    {
        // null = el servidor no tiene registro de este usuario todavía
        // (aún no sincronizó desde ninguna tablet) — no es lo mismo que
        // "inactivo", así que el cliente no debe forzar cierre de sesión
        // en este caso, solo cuando es explícitamente false.
        public bool? IsActive { get; set; }
    }
}