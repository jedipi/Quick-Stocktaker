using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.Models.Sqlite
{
    public class SQLiteDB
    {
        SQLiteAsyncConnection database;
        public SQLiteAsyncConnection Database
        {
            get;
            set;

        }

        public const SQLite.SQLiteOpenFlags Flags =
        // open the database in read/write mode
        SQLite.SQLiteOpenFlags.ReadWrite |
        // create the database if it doesn't exist
        SQLite.SQLiteOpenFlags.Create |
        // enable multi-threaded database access
        SQLite.SQLiteOpenFlags.SharedCache;

        public string DatabasePath { get; set; }

        public SQLiteDB(string databasePath)
        {
            DatabasePath = databasePath;
            Init();
        }
        void Init()
        {
            if (Database is not null)
                return;
            try
            {
                Database = new SQLiteAsyncConnection(DatabasePath, Flags);
            }
            catch(Exception e)
            {
                var a = e.Message;
            }
        }
    }
}
