
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    /// <summary>
    /// Export stocktake data as a CSV file
    /// </summary>
    public class CsvExportService : ICsvExportService
    {
        private readonly IStocktakeOperationsService _stocktakeOperations;
        private readonly IAppPreferences _preferences;
        private readonly IAppFileSystem _fileSystem;

        public CsvExportService(
            IStocktakeOperationsService stocktakeOperations,
            IAppPreferences preferences,
            IAppFileSystem fileSystem)
        {
            _stocktakeOperations = stocktakeOperations;
            _preferences = preferences;
            _fileSystem = fileSystem;
        }

        public async Task<StocktakeExport> CreateExportAsync(
            Stocktake stocktake,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deviceId = _preferences.GetString(Constants.DeviceId, "");
            var data = await _stocktakeOperations
                    .GetItemsAsync(stocktake.Id, cancellationToken)
                    .ConfigureAwait(false);
            if (data.Count == 0)
                return null;

            var dir = _fileSystem.AppDataDirectory;
            var filePath = Path.Combine(
                dir,
                $"Stocktake-{stocktake.Site}-{deviceId}-{DateTime.Now.ToString("yyMMdd-HHmmss")}.csv");
            var tempFilePath = $"{filePath}.{Guid.NewGuid():N}.tmp";

            try
            {
                await using (var textWriter = new StreamWriter(tempFilePath, false))
                {
                    using var csv = new CsvWriter(textWriter, CultureInfo.CurrentCulture);
                    csv.Context.RegisterClassMap<StocktakeItemCsvMap>();
                    await csv.WriteRecordsAsync(data, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(tempFilePath, filePath, true);
                return new StocktakeExport(new FileInfo(filePath));
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        private sealed class StocktakeItemCsvMap : ClassMap<StocktakeItem>
        {
            public StocktakeItemCsvMap()
            {
                Map(item => item.DeviceId).Index(0);
                Map(item => item.StocktakeNumber).Index(1);
                Map(item => item.Site).Index(2);
                Map(item => item.BayLocation).Index(3);
                Map(item => item.Barcode).Index(4);
                Map(item => item.Description).Index(5);
                Map(item => item.Qty).Index(6);
                Map(item => item.StocktakeDate).Index(7);
                Map(item => item.InsertedAt).Index(8);
                Map(item => item.UpdatedAt).Index(9);
                Map(item => item.Id).Index(10);
            }
        }


    }
}
