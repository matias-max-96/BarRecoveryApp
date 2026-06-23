using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Security
{
    [Table("RolePermissions")]
    public class RolePermission : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string RoleId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string PermissionId {  get; set; } = string.Empty;
    }
}
