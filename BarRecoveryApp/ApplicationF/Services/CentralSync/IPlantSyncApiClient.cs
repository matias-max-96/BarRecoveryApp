using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public interface IPlantSyncApiClient
    {
        // null si falla (red, no autenticado, etc.) — el llamador decide
        // si reintentar más tarde.
        Task<List<PlantSyncDto>?> GetChangedSinceAsync(DateTime? since);

        Task<PlantPushOutcome> PushAsync(PlantSyncDto dto);
    }

    public class PlantPushOutcome
    {
        public PlantPushResult Result { get; set; }

        // Si Result == Conflict, esta es la versión real del servidor —
        // el llamador debe adoptarla localmente en vez de la que mandó.
        public PlantSyncDto? ServerVersion { get; set; }
    }

    public enum PlantPushResult
    {
        Success,
        Conflict,
        NotAuthenticated,
        NetworkError
    }
}
