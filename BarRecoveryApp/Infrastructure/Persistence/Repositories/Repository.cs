using System.Linq.Expressions;
using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Infrastructure.Persistence.Repositories
{
    public class Repository<T> : IRepository<T> where T : EntityBase, new()
    {
        private readonly IDatabaseService _databaseService;

        public Repository(IDatabaseService databaseService)
        {
            _databaseService = databaseService
                ?? throw new ArgumentNullException(nameof(databaseService));
        }
        private async Task<SQLiteAsyncConnection> GetDbAsync()
        {
            return await _databaseService.GetConnectionAsync();
        }

        public async Task<List<T>> GetAllAsync()
        {
            var db = await GetDbAsync();

            return await db.Table<T>().ToListAsync();
        }

        public async Task<List<T>> GetActiveAsync()
        {
            var db = await GetDbAsync();


            return await db.Table<T>()
                            .Where(x => x.IsActive)
                            .ToListAsync();
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            var db = await GetDbAsync();

            return await db.Table<T>()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            var db = await GetDbAsync();

            return await db.Table<T>()
                .Where(predicate).FirstOrDefaultAsync();
        }

        public async Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        {
            var db = await GetDbAsync();

            return await db.Table<T>()
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<int> InsertAsync(T entity)
        {
            var db = await GetDbAsync();

            if (string.IsNullOrWhiteSpace(entity.Id))
                entity.Id = Guid.NewGuid().ToString();

            entity.CreatedAtUtc = DateTime.Now;
            entity.UpdatedAtUtc = DateTime.Now;

            return await db.InsertAsync(entity);
        }

        public async Task<int> UpdateAsync(T entity)
        {
            var db = await GetDbAsync();

            entity.UpdatedAtUtc = DateTime.Now;

            return await db.UpdateAsync(entity);
        }

        public async Task<int> SaveAsync(T entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
                return await InsertAsync(entity);

            var existing = await GetByIdAsync(entity.Id);

            if (existing != null)
                return await UpdateAsync(entity); // existe -> actualizar

            return await InsertAsync(entity); // no existe -> insertar
        }


        public async Task<int> DeleteAsync(T entity)
        {
            var db = await GetDbAsync();

            return await db.DeleteAsync(entity);
        }

        public async Task<int> SoftDeleteAsync(T entity)
        {
            entity.IsActive = false;
            entity.UpdatedAtUtc = DateTime.Now;

            return await UpdateAsync(entity);
        }

        public async Task<int> CountAsync()
        {
            var db = await GetDbAsync();

            return await db.Table<T>().CountAsync();
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            var db = await GetDbAsync();

            var item = await db.Table<T>()
                .Where(predicate)
                .FirstOrDefaultAsync();

            return item is not null;
        }

    }
}