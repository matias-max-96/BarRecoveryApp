using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public enum BarPushResult
    {
        Success,
        Conflict,
        NotAuthenticated,
        NetworkError
    }

    public class BarPushOutcome
    {
        public BarPushResult Result { get; set; }

        public BarSyncDto? ServerVersion { get; set; }
    }

    public interface IBarSyncApiClient
    {
        Task<List<BarSyncDto>?> GetChangedSinceAsync(DateTime? since);

        Task<BarPushOutcome> PushAsync(BarSyncDto dto);
    }
}