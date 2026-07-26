namespace BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos
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

    public class UserActiveStatusDto
    {
        public bool? IsActive { get; set; }
    }
}