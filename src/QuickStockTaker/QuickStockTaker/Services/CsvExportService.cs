using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Acr.UserDialogs;
using CsvHelper;
using CsvHelper.Configuration;
using NLog;
using QuickStockTaker.Data;
using QuickStockTaker.DataAccess;
using QuickStockTaker.Interfaces;
using QuickStockTaker.Models;
using Xamarin.Essentials;

namespace QuickStockTaker.Services
{
    /// <summary>
    /// Export stocktake data as a CSV file
    /// </summary>
    public class CsvExportService : ICsvExport
    {
        private IUserDialogs _dialogs;
        private readonly NLog.ILogger _logger;
        private IDBConnection _dbConnection;
        private static object locker = new object();

        // the exported file info
        public FileInfo ExportedFile { get; set; }

        /// <summary>
        /// Export stocktake data as a CSV file
        /// </summary>
        public CsvExportService(IUserDialogs dialogs, ILogger logger, IDBConnection dbConnection)
        {
            _dialogs = dialogs;
            _logger = logger;
            _dbConnection = dbConnection;
        }
        
        
        public async Task Export()
        {
            // get all stocktake data
            var data = await _dbConnection.Database.Table<StocktakeItem>().ToListAsync();

            // There is no stocktake data to export
            if (data.Count == 0)
                return;

            var site = Preferences.Get(Constants.Site, "");
            var deviceId = Preferences.Get(Constants.DeviceId, 0);
            
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var filePath = Path.Combine(dir, $"{DateTime.Now.ToString("yyMMdd-HHmmss")}-{site}-{deviceId}-Stocktake.csv");
            
            // write data into a csv file using csvhelper
            await using (var textWriter = new StreamWriter(filePath, false))
            {
                try
                {
                    var csv = new CsvWriter(textWriter, CultureInfo.CurrentCulture);
                    csv.WriteRecords(data);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "csv export fail.");
                    return;
                }
            }
            
            ExportedFile = new FileInfo(filePath);
        }

        
    }
}
