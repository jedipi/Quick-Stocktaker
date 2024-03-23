using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.Repositories
{
    public class SQLiteRepository<T> : ISQLiteRepository<T> where T : BaseModel, new()
    {
        private static readonly object locker = new object();
        private bool disposedValue;
        public SQLiteAsyncConnection Connection { get; set; }
        public SQLiteRepository(SQLiteDB db)
        {
            try
            {
                Connection = db.Database;
                Connection.CreateTableAsync<T>().Wait();
            }
            catch (Exception ex)
            {
                var s = ex.Message;
                //Log.Error(s, ex);
            }

        }

        public async Task<int> InsertAsync(T entity)
        {
            return await Connection.InsertAsync(entity);
        }

        public async Task<int> InsertAllAsync(List<T> entities)
        {
            return await Connection.InsertAllAsync(entities);
        }

        public async Task<int> DeleteAsync(T entity)
        {
            return await Connection.DeleteAsync(entity);
        }
        public async Task<int> DeleteAsync(Expression<Func<T, bool>> expression)
        {
            return await Connection.Table<T>().DeleteAsync(expression);
        }


        public async Task<int> UpdateAsync(T entity)
        {
            return await Connection.UpdateAsync(entity);
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await Connection.Table<T>().FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await Connection.Table<T>().ToListAsync();
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> expression)
        {
            return await Connection.Table<T>().Where(expression).ToListAsync();
        }

        public async Task DropandRecreateTable()
        {
            await Connection.RunInTransactionAsync((trans) =>
            {
                trans.DropTable<T>();
                trans.CreateTable<T>();
            }
           );
        }

        //public async Task<int> ExecuteAsync(string query, params object[] objects)
        //{
        //    return await Connection.ExecuteAsync(query, objects);
        //    //await connection.RunInTransactionAsync((trans) =>
        //    //{
        //    //    trans.Execute(query, objects);
        //    //}
        //    //    );
        //}
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (Connection != null)
                    {
                        Connection = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SQLiteRepository()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
