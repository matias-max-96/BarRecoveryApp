using System.Linq.Expressions;
using BarRecoveryApp.Models.Base;

namespace BarRecoveryApp.Infrastructure.Persistence.Repositories
{
    public interface IRepository<T> where T : EntityBase, new()
    {
        Task<List<T>> GetAllAsync();

        Task<List<T>> GetActiveAsync();

        Task<T?> GetByIdAsync(string id);

        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate);

        Task<int> InsertAsync(T entity);

        Task<int> UpdateAsync(T entity);

        Task<int> SaveAsync(T entity);

        Task<int> DeleteAsync(T entity);

        Task<int> SoftDeleteAsync(T entity);

        Task<int> CountAsync();

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    }
}
