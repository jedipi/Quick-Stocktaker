using FluentAssertions;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.UnitTest;

public class CsvExportServiceTests
{
    [Fact]
    public async Task Export_WhenNoRowsExist_DoesNotCreateExportedFile()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-csv-empty-");
        try
        {
            var service = CreateService([], tempDirectory.FullName);

            await service.Export();

            service.ExportedFile.Should().BeNull();
            Directory.GetFiles(tempDirectory.FullName).Should().BeEmpty();
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Export_WhenRowsExist_CreatesCsvWithCurrentFilenameAndContentShape()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-csv-data-");
        try
        {
            var item = new StocktakeItem
            {
                Id = 42,
                DeviceId = "SCANNER-01",
                StocktakeNumber = "ST-100",
                Site = "WH-A",
                BayLocation = "BAY-1",
                Barcode = "ABC123",
                Description = "Sample item",
                Qty = 3,
                StocktakeDate = "1/06/2026",
                InsertedAt = "1/06/2026 10:00 PM",
                UpdatedAt = "1/06/2026 10:00 PM"
            };
            var service = CreateService([item], tempDirectory.FullName);

            await service.Export();

            service.ExportedFile.Should().NotBeNull();
            service.ExportedFile!.Exists.Should().BeTrue();
            service.ExportedFile.DirectoryName.Should().Be(tempDirectory.FullName);
            service.ExportedFile.Name.Should().StartWith("Stocktake-WH-A-SCANNER-01-");
            service.ExportedFile.Extension.Should().Be(".csv");

            var content = await File.ReadAllTextAsync(service.ExportedFile.FullName, TestContext.Current.CancellationToken);
            content.Should().Contain("DeviceId");
            content.Should().Contain("SCANNER-01");
            content.Should().Contain("ST-100");
            content.Should().Contain("WH-A");
            content.Should().Contain("BAY-1");
            content.Should().Contain("ABC123");
            content.Should().Contain("3");
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    private static CsvExportService CreateService(IReadOnlyCollection<StocktakeItem> items, string appDataDirectory)
    {
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.Site, "WH-A");
        preferences.Set(Constants.DeviceId, "SCANNER-01");

        return new CsvExportService(
            new TestStocktakeItemRepository(items),
            preferences,
            new TestAppFileSystem(appDataDirectory));
    }
}
