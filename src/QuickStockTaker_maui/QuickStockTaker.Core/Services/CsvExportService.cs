
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    /// <summary>
    /// Export stocktake data as a CSV file
    /// </summary>
    public class CsvExportService : ICsvExportService
    {
        //private IUserDialogs _dialogs;
        //private readonly NLog.ILogger _logger;
        //private IDBConnection _dbConnection;
        private static object locker = new object();

        // the exported file info
        public FileInfo ExportedFile { get; set; }

        private ISQLiteRepository<StocktakeItem> _stocktakeItemRepo;

        /// <summary>
        /// Export stocktake data as a CSV file
        /// </summary>
        public CsvExportService(ISQLiteRepository<StocktakeItem> stocktakeItemRepo)
        {
            //_dialogs = dialogs;
            //_logger = logger;
            _stocktakeItemRepo = stocktakeItemRepo;
        }
        
        
        public async Task Export()
        {
            // get all stocktake data
            var data = await _stocktakeItemRepo.GetAllAsync();

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
                    //_logger.Error(e, "csv export fail.");
                    return;
                }
            }
            
            ExportedFile = new FileInfo(filePath);
        }

        
    }
}
