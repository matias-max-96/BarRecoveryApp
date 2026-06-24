using SQLite;

namespace BarRecoveryApp.Infrastructure.Persistence
{
    public interface IDatabaseService
    {
        Task InitAsync();
        Task<SQLiteAsyncConnection> GetConnectionAsync();
        string GetDatabasePath();
    }
}
