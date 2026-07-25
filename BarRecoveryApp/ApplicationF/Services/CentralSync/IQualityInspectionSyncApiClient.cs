using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public enum QualityInspectionPushResult
    {
        Success,
        NotAuthenticated,
        NetworkError
    }

    public interface IQualityInspectionSyncApiClient
    {
        Task<List<QualityInspectionSyncDto>?> GetChangedSinceAsync(DateTime? since);

        Task<QualityInspectionPushResult> PushAsync(QualityInspectionSyncDto dto);
    }
}