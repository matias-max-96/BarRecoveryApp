using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public enum RecoveryWorkReportPushResult
    {
        Success,
        NotAuthenticated,
        NetworkError
    }

    public interface IRecoveryWorkReportSyncApiClient
    {
        Task<List<RecoveryWorkReportSyncDto>?> GetChangedSinceAsync(DateTime? since);

        Task<RecoveryWorkReportPushResult> PushAsync(RecoveryWorkReportSyncDto dto);
    }
}