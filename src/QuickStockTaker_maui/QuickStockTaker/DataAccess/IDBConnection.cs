using SQLite;

namespace QuickStockTaker.DataAccess
{
    public interface IDBConnection
    { 
        public SQLiteAsyncConnection Database { get; }
        //public SQLiteAsyncConnection DbConnection();
    }
}
