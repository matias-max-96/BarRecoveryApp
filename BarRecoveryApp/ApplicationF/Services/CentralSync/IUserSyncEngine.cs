namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class UserSyncRunResult
    {
        public int Pulled { get; set; }

        public int Pushed { get; set; }

        public int PushConflicts { get; set; }

        public bool NotConfigured { get; set; }
    }

    public interface IUserSyncEngine
    {
        Task<UserSyncRunResult> SyncAsync();
    }
}