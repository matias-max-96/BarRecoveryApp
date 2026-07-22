namespace BarRecoveryApp.Api.Services
{
    public interface IJwtTokenService
    {
        (string Token, DateTime ExpiresAtUtc) GenerateToken(string userId, string username);
    }
}
