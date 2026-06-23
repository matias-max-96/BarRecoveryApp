using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Security
{
    [Table("Users")]
    public class User : EntityBase
    {
        [Indexed(Unique = true)]
        [MaxLength(120)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(150)]
        public string DisplayName { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string RoleId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string PinHash { get; set; } = string.Empty;

        [MaxLength(200)]
        public string PinSalt { get; set; } = string.Empty;

        public bool IsPinEnabled { get; set; } = true;

        [Indexed]
        [MaxLength(36)]
        public string CreatedByUserId { get; set; } = string.Empty;

        [MaxLength(36)]
        public string? UpdatedByUserId { get; set; }

        public bool MustChangePin { get; set; } = false;

    }
}
