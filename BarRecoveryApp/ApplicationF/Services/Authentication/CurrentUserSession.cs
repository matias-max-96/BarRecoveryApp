using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Authentication
{
    public class CurrentUserSession
    {
        public string UserId { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string RoleId { get; set; } = string.Empty;

        public string RoleCode { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public List<string> PermissionCodes { get; set; } = new();

        public DateTime LoginAt { get; set; } = DateTime.Now;

        public DateTime LastActivityAt { get; set; } = DateTime.Now;

        public bool IsAuthenticated { get; set; }

        public bool MustChangePin {  get; set; }

        public bool HasPermission(string permissionCode)
        {
            return PermissionCodes.Contains(permissionCode);
        }
    }
}
