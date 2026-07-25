using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public enum BarReturnReceiptPushResult
    {
        Success,
        NotAuthenticated,
        NetworkError
    }

    public interface IBarReturnReceiptSyncApiClient
    {
        Task<List<BarReturnReceiptSyncDto>?> GetChangedSinceAsync(DateTime? since);

        Task<BarReturnReceiptPushResult> PushAsync(BarReturnReceiptSyncDto dto);
    }
}