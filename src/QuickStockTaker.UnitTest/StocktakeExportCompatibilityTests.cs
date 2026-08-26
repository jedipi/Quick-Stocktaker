using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeExportCompatibilityTests
{
    [Fact]
    public async Task CreateExportAsync_UsesCurrentInvariantCsvContract()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-csv-contract-");
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var preferences = new TestAppPreferences();
            preferences.Set(Constants.Site, "WH-A");
            preferences.Set(Constants.DeviceId, "SCANNER-01");
            var exporter = new CsvExportService(
                new TestStocktakeItemRepository(
                [
                    new StocktakeItem
                    {
                        Id = 42,
                        DeviceId = "SCANNER-01",
                        StocktakeNumber = "ST-100",
                        Site = "WH-A",
                        BayLocation = "BAY-1",
                        Barcode = "ABC123",
                        Description = "Sample item",
                        Qty = 3,
                        StocktakeDate = "2026-06-01",
                        InsertedAt = "2026-06-01 22:00:00",
                        UpdatedAt = "2026-06-01 22:00:00"
                    }
                ]),
                preferences,
                new TestAppFileSystem(tempDirectory.FullName));
            IStocktakeDeliveryWorkflow workflow = new StocktakeDeliveryWorkflow(
                exporter,
                NullLogger<StocktakeDeliveryWorkflow>.Instance);

            var result = await workflow.CreateExportAsync(TestContext.Current.CancellationToken);

            var content = await File.ReadAllTextAsync(result.Export!.File.FullName, TestContext.Current.CancellationToken);
            content.Replace("\r\n", "\n").Should().Be(
                "DeviceId,StocktakeNumber,Site,BayLocation,Barcode,Description,Qty,StocktakeDate,InsertedAt,UpdatedAt,Id\n" +
                "SCANNER-01,ST-100,WH-A,BAY-1,ABC123,Sample item,3,2026-06-01,2026-06-01 22:00:00,2026-06-01 22:00:00,42\n");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            tempDirectory.Delete(recursive: true);
        }
    }
}
