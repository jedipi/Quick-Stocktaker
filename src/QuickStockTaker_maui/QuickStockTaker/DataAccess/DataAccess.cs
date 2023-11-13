using QuickStockTaker.Models;
using SQLite;

namespace QuickStockTaker.DataAccess
{
    /// <summary>
    /// Database functions
    /// </summary>
    public class SQLiteDb
    {
        private readonly NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();
        private IDBConnection _dbConnection;

        public SQLiteDb(IDBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        /// <summary>
        /// Create database tables
        /// </summary>
        /// <returns></returns>
        public async Task Create()
        {
            try
            {
                var db = _dbConnection.Database;
                await db.CreateTableAsync<StocktakeItem>();
            }
            catch (Exception e)
            {
                _logger.Error(e,"DB fail.");
                throw;
            }
            
        }
    }
}
