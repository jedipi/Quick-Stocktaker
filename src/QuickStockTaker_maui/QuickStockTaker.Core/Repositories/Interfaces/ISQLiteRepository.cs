using QuickStockTaker.Core.Models.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.Repositories.Interfaces
{
    public interface ISQLiteRepository<T> : IDisposable where T : BaseModel, new()
    {
        Task<int> InsertAsync(T entity);
        Task<int> InsertAllAsync(List<T> entities);
        Task<int> DeleteAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<int> DeleteAsync(Expression<Func<T, bool>> expression);
        Task<T> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task<List<T>> FindAsync(Expression<Func<T, bool>> expression);

        Task DropandRecreateTable();
    }
}
