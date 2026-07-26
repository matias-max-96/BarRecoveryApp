namespace BarRecoveryApp.Api.Entities
{
    public class User : EntityBase
    {
        public string Username { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        // RoleCode, no RoleId: los Role.Id se generan independientes en el
        // seeder de cada tablet (no sincronizan), así que el mismo GUID no
        // significa lo mismo entre tablets. El Code ("ADMIN", "OPERATOR",
        // etc.) sí es consistente, porque el seeder siempre crea los mismos
        // códigos en todas las instalaciones.
        public string RoleCode { get; set; } = string.Empty;

        // Viaja el hash, nunca el PIN en texto plano — aun así, es un dato
        // sensible. Ver nota de seguridad: hoy el backend corre HTTP plano
        // en el emulador; antes de dispositivos reales fuera de la red de
        // pruebas, esto necesita HTTPS con certificado válido.
        public string PinHash { get; set; } = string.Empty;

        public string PinSalt { get; set; } = string.Empty;

        public bool IsPinEnabled { get; set; }

        public bool MustChangePin { get; set; }

        public string CreatedByUserId { get; set; } = string.Empty;

        public string? UpdatedByUserId { get; set; }
    }
}