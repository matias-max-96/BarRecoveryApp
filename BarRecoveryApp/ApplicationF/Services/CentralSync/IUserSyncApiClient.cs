using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public enum UserPushResult
    {
        Success,
        Conflict,
        NotAuthenticated,
        NetworkError
    }

    public class UserPushOutcome
    {
        public UserPushResult Result { get; set; }

        public UserSyncDto? ServerVersion { get; set; }
    }

    public interface IUserSyncApiClient
    {
        Task<List<UserSyncDto>?> GetChangedSinceAsync(DateTime? since);

        Task<UserPushOutcome> PushAsync(UserSyncDto dto);

        // Verificación oportunista post-login. Devuelve null si no se pudo
        // determinar (sin red, sin config, usuario aún no sincronizado al
        // servidor) — en ese caso el llamador NO debe cerrar la sesión.
        Task<bool?> CheckUserActiveAsync(string userId);
    }
}