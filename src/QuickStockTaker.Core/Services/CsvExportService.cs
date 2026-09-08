
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
        private readonly IExportPresetService _presets;

        public CsvExportService(
            IStocktakeOperationsService stocktakeOperations,
            IAppPreferences preferences,
            IAppFileSystem fileSystem,
            IExportPresetService presets)
        {
            _stocktakeOperations = stocktakeOperations;
            _preferences = preferences;
            _fileSystem = fileSystem;
            _presets = presets;
        }

        public async Task<StocktakeExport> CreateExportAsync(
            Stocktake stocktake,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var selection = await _presets.GetSelectionAsync().ConfigureAwait(false);
            if (selection.Notice != null)
                throw new ExportPresetException(selection.Notice + " Please retry using Default CSV.");
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
                    await WriteAsync(textWriter, data, stocktake, selection.Preset, cancellationToken).ConfigureAwait(false);
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

        internal static async Task WriteAsync(TextWriter writer, IEnumerable<StocktakeItem> items,
            Stocktake stocktake, ExportPreset preset, CancellationToken cancellationToken)
        {
            if (preset == null)
            {
                using var defaultCsv = new CsvWriter(writer, CultureInfo.CurrentCulture, leaveOpen: true);
                defaultCsv.Context.RegisterClassMap<StocktakeItemCsvMap>();
                await defaultCsv.WriteRecordsAsync(items, cancellationToken).ConfigureAwait(false);
                return;
            }

            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = preset.Delimiter,
                HasHeaderRecord = preset.IncludeHeader
            }, leaveOpen: true);
            var columns = preset.Columns.Where(column => column.Included).ToArray();
            if (preset.IncludeHeader)
            {
                foreach (var column in columns)
                    csv.WriteField(column.Header);
                await csv.NextRecordAsync().ConfigureAwait(false);
            }
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var column in columns)
                {
                    var value = column.Field switch
                    {
                        "DeviceId" => item.DeviceId,
                        "StocktakeNumber" => item.StocktakeNumber,
                        "Site" => item.Site,
                        "BayLocation" => item.BayLocation,
                        "Barcode" => item.Barcode,
                        "Description" => item.Description,
                        "Qty" => item.Qty.ToString(CultureInfo.InvariantCulture),
                        "StocktakeDate" => stocktake.StocktakeDate.ToString(preset.DateFormat, CultureInfo.InvariantCulture),
                        "InsertedAt" => FormatTimestamp(item.InsertedAt, preset.DateFormat, column.Field, item.Id),
                        "UpdatedAt" => FormatTimestamp(item.UpdatedAt, preset.DateFormat, column.Field, item.Id),
                        "Id" => item.Id.ToString(CultureInfo.InvariantCulture),
                        _ => throw new ExportPresetException("Unknown export column. Edit the preset or choose Default CSV.")
                    };
                    csv.WriteField(value);
                }
                await csv.NextRecordAsync().ConfigureAwait(false);
            }
        }

        private static string FormatTimestamp(string value, string dateFormat, string field, int itemId)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            // Legacy timestamps have no saved culture. Accept only an unambiguous interpretation.
            var candidates = new HashSet<DateTime>();
            foreach (var culture in new[] { CultureInfo.CurrentCulture, CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("en-GB") })
            {
                string[] formats =
                [
                    "s", "O", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm",
                    culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern,
                    culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern,
                    "d/M/yyyy H:mm", "d/M/yyyy H:mm:ss", "d/M/yyyy h:mm tt", "d/M/yyyy h:mm:ss tt",
                    "M/d/yyyy H:mm", "M/d/yyyy H:mm:ss", "M/d/yyyy h:mm tt", "M/d/yyyy h:mm:ss tt"
                ];
                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(value, format, culture, DateTimeStyles.None, out var parsed))
                        candidates.Add(parsed);
                }
            }
            if (candidates.Count != 1)
                throw new ExportPresetException($"Item {itemId}: {field} cannot be interpreted reliably. Choose Default CSV to export the original date values, or hide this column.");
            return candidates.Single().ToString(dateFormat + " HH:mm:ss", CultureInfo.InvariantCulture);
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
