
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
        private static object locker = new();

        // the exported file info
        public FileInfo ExportedFile { get; set; }

        private readonly ISQLiteRepository<StocktakeItem> _stocktakeItemRepo;
        private readonly IAppPreferences _preferences;
        private readonly IAppFileSystem _fileSystem;

        /// <summary>
        /// Export stocktake data as a CSV file
        /// </summary>
        public CsvExportService(
            ISQLiteRepository<StocktakeItem> stocktakeItemRepo,
            IAppPreferences preferences,
            IAppFileSystem fileSystem)
        {
            //_dialogs = dialogs;
            //_logger = logger;
            _stocktakeItemRepo = stocktakeItemRepo;
            _preferences = preferences;
            _fileSystem = fileSystem;
        }


        public async Task Export()
        {
            // get all stocktake data
            var data = await _stocktakeItemRepo.GetAllAsync();

            // There is no stocktake data to export
            if (data.Count == 0)
                return;

            var site = _preferences.GetString(Constants.Site, "");
            var deviceId = _preferences.GetString(Constants.DeviceId, "");

            var dir = _fileSystem.AppDataDirectory;
            var filePath = Path.Combine(dir, $"Stocktake-{site}-{deviceId}-{DateTime.Now.ToString("yyMMdd-HHmmss")}.csv");

            // write data into a csv file using csvhelper
            await using (var textWriter = new StreamWriter(filePath, false))
            {
                try
                {
                    var csv = new CsvWriter(textWriter, CultureInfo.CurrentCulture);
                    csv.WriteRecords(data);
                }
                catch (Exception)
                {
                    //_logger.Error(e, "csv export fail.");
                    return;
                }
            }

            ExportedFile = new FileInfo(filePath);
        }


    }
}
