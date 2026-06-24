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
                throw new InvalidOperationException("No existe conexión a la bse de datos de datos.");

            //Users / security
            await _database.CreateTableAsync<Role>();
            await _database.CreateTableAsync<Permission>();
            await _database.CreateTableAsync<RolePermission>();
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<AuditLog>();

            //Catalog
            await _database.CreateTableAsync<Plant>();
            await _database.CreateTableAsync<BarType>();
            await _database.CreateTableAsync<Activity>();
            await _database.CreateTableAsync<Supply>();
            await _database.CreateTableAsync<BarRecoveryPolicy>();
            await _database.CreateTableAsync<BarAttributeDefinition>();

            //Operation
            await _database.CreateTableAsync<Bar>();
            await _database.CreateTableAsync<BarAttributeValue>();
            await _database.CreateTableAsync<RecoveryRecord>();
            await _database.CreateTableAsync<RecoveryRecordSupply>();
            await _database.CreateTableAsync<QualityInspection>();
            await _database.CreateTableAsync<Shipment>();
            await _database.CreateTableAsync<ShipmentBar>();

            //DB future sync
            await _database.CreateTableAsync<SyncQueueItem>();
            await _database.CreateTableAsync<SyncState>();
        }
    }
}
