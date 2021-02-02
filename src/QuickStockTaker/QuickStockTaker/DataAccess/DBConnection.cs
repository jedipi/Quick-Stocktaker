using System;
using System.IO;
//using QuickStockTaker.Interface;
using SQLite;
using Xamarin.Forms;

namespace QuickStockTaker.DataAccess
{
    /// <summary>
    /// Initialise database connection
    /// </summary>
    public class DBConnection : IDBConnection
    {
        SQLiteAsyncConnection database;

        // singleton to keep the database open for the whole app
        public SQLiteAsyncConnection Database
        {
            get
            {
                if (database == null)
                {
                    database = DbConnection();
                }

                return database;
            }
        }


        /// <summary>
        /// Get database connection
        /// </summary>
        /// <returns>database connection</returns>
        private SQLiteAsyncConnection DbConnection()
        {
            var dbPath = "StockTacker.db3";

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    var personalFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    var libraryFolder = Path.Combine(personalFolder, "..", "Library");
                    dbPath = Path.Combine(libraryFolder, dbPath);
                    break;
                case Device.Android:
                    dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dbPath);
                    break;
                //case Device.UWP:
                //    dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbPath);
                //    break;
            }

            return new SQLiteAsyncConnection(dbPath);
        }
    }
}
