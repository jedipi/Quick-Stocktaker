using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeDeliveryWorkflowTests
{
    [Fact]
    public async Task CreateExportAsync_WhenCancelled_ReturnsCancelledWithoutCreatingFile()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-delivery-cancelled-");
        try
        {
            var exporter = new CsvExportService(
                new TestStocktakeItemRepository(
                [
                    new StocktakeItem { Barcode = "ABC123", Qty = 1 }
                ]),
                new TestAppPreferences(),
                new TestAppFileSystem(tempDirectory.FullName));
            IStocktakeDeliveryWorkflow workflow = new StocktakeDeliveryWorkflow(
                exporter,
                NullLogger<StocktakeDeliveryWorkflow>.Instance);
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            var result = await workflow.CreateExportAsync(cancellation.Token);

            result.Status.Should().Be(StocktakeDeliveryStatus.Cancelled);
            result.Export.Should().BeNull();
            Directory.GetFiles(tempDirectory.FullName).Should().BeEmpty();
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CreateExportAsync_WhenStocktakeHasNoItems_ReturnsNoStocktakeData()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-delivery-empty-");
        try
        {
            var exporter = new CsvExportService(
                new TestStocktakeItemRepository([]),
                new TestAppPreferences(),
                new TestAppFileSystem(tempDirectory.FullName));
            IStocktakeDeliveryWorkflow workflow = new StocktakeDeliveryWorkflow(
                exporter,
                NullLogger<StocktakeDeliveryWorkflow>.Instance);

            var result = await workflow.CreateExportAsync(TestContext.Current.CancellationToken);

            result.Status.Should().Be(StocktakeDeliveryStatus.NoStocktakeData);
            result.Export.Should().BeNull();
            Directory.GetFiles(tempDirectory.FullName).Should().BeEmpty();
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CreateExportAsync_WhenStocktakeHasItems_ReturnsFreshStocktakeExport()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-delivery-success-");
        try
        {
            var preferences = new TestAppPreferences();
            preferences.Set(Constants.Site, "WH-A");
            preferences.Set(Constants.DeviceId, "SCANNER-01");

            var exporter = new CsvExportService(
                new TestStocktakeItemRepository(
                [
                    new StocktakeItem
                    {
                        Barcode = "ABC123",
                        BayLocation = "BAY-1",
                        Qty = 3
                    }
                ]),
                preferences,
                new TestAppFileSystem(tempDirectory.FullName));
            IStocktakeDeliveryWorkflow workflow = new StocktakeDeliveryWorkflow(
                exporter,
                NullLogger<StocktakeDeliveryWorkflow>.Instance);

            var result = await workflow.CreateExportAsync(TestContext.Current.CancellationToken);

            result.Status.Should().Be(StocktakeDeliveryStatus.Succeeded);
            result.Export.Should().NotBeNull();
            result.Export!.File.Exists.Should().BeTrue();
            result.Export.File.Name.Should().StartWith("Stocktake-WH-A-SCANNER-01-");
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
