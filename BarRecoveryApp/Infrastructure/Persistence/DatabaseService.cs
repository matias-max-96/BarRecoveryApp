using SQLite;
using BarRecoveryApp.Models.Security;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.Infrastructure.Persistence
{
    public class DatabaseService : IDatabaseService
    {
        private const string DatabaseFilename = "BarRecoveryApp.db3";

        private SQLiteAsyncConnection? _database;

        private readonly SemaphoreSlim _initSemaphore = new(1, 1);

        private bool _isInitialized;

        private static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

        private static readonly SQLiteOpenFlags Flags =
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache;

        public async Task InitAsync()
        {
            if (_isInitialized)
                return;

            await _initSemaphore.WaitAsync();

            try
            {
                if (_isInitialized)
                    return;

                _database = new SQLiteAsyncConnection(DatabasePath, Flags);

                await CreateTableAsync();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        public string GetDatabasePath()
        {
            return DatabasePath;
        }

        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await InitAsync();

            if (_database is null)
                throw new InvalidOperationException("La base de datos no fue inicializada correctamente.");

            return _database;
        }

        private async Task CreateTableAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("No existe conexión a la base de datos.");

            // Users / security
            await SafeCreateTableAsync<Role>();
            await SafeCreateTableAsync<Permission>();
            await SafeCreateTableAsync<RolePermission>();
            await SafeCreateTableAsync<User>();
            await SafeCreateTableAsync<AuditLog>();

            // Catalog
            await SafeCreateTableAsync<Plant>();
            await SafeCreateTableAsync<BarType>();
            await SafeCreateTableAsync<Activity>();
            await SafeCreateTableAsync<Supply>();
            await SafeCreateTableAsync<BarRecoveryPolicy>();
            await SafeCreateTableAsync<BarAttributeDefinition>();

            // Operation
            await SafeCreateTableAsync<Bar>();
            await SafeCreateTableAsync<BarAttributeValue>();
            await SafeCreateTableAsync<RecoveryWorkActivity>();
            await SafeCreateTableAsync<RecoveryWorkReport>();
            await SafeCreateTableAsync<RecoveryWorkSupply>();
            await SafeCreateTableAsync<QualityInspection>();
            await SafeCreateTableAsync<Shipment>();
            await SafeCreateTableAsync<ShipmentBar>();
            await SafeCreateTableAsync<BarReturnReceipt>();
            await SafeCreateTableAsync<BarReturnReceiptBar>();

            // DB future sync
            await SafeCreateTableAsync<SyncQueueItem>();
            await SafeCreateTableAsync<SyncState>();
        }

        private async Task SafeCreateTableAsync<T>() where T : new()
        {
            if (_database is null)
                throw new InvalidOperationException("No existe conexión a la base de datos.");

            try
            {
                await _database.CreateTableAsync<T>();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}