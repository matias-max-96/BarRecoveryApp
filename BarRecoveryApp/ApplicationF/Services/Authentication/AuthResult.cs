using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Authentication
{
    public class AuthResult
    {
        public bool Success { get; set; }

        public string Message { get; set;  }

        public User? User { get; set; }

        public Role? Role { get; set; }

        public List<Permission> Permissions { get; set; } = new();

        public bool MustChangePin { get; set; }

        public static AuthResult Fail(string message)
        {
            return new AuthResult
            {
                Success = false,
                Message = message
            };
        }

        public static AuthResult Ok(
            User user,
            Role role,
            List<Permission> permissions,
            bool mustChangePin)
        {
            return new AuthResult
            {
                Success = true,
                Message = "Autentificación Correcta,",
                User = user,
                Role = role,
                Permissions = permissions,
                MustChangePin = mustChangePin
            };
        }
    }
}
