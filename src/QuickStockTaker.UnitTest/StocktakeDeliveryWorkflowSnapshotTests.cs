using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeDeliveryWorkflowSnapshotTests
{
    [Fact]
    public async Task CreateExportAsync_WhenIdentityChangesDuringRead_UsesIdentityCapturedAtStart()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-delivery-snapshot-");
        var snapshotStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSnapshot = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var preferences = new TestAppPreferences();
            preferences.Set(Constants.Site, "WH-OLD");
            preferences.Set(Constants.DeviceId, "SCANNER-OLD");
            var repository = Substitute.For<ISQLiteRepository<StocktakeItem>>();
            repository.GetAllAsync().Returns(_ => ReadSnapshotAsync());
            var exporter = new CsvExportService(
                repository,
                preferences,
                new TestAppFileSystem(tempDirectory.FullName));
            IStocktakeDeliveryWorkflow workflow = new StocktakeDeliveryWorkflow(
                exporter,
                NullLogger<StocktakeDeliveryWorkflow>.Instance);

            var operation = workflow.CreateExportAsync(TestContext.Current.CancellationToken);
            await snapshotStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
            preferences.Set(Constants.Site, "WH-NEW");
            preferences.Set(Constants.DeviceId, "SCANNER-NEW");
            releaseSnapshot.TrySetResult();

            var result = await operation;

            result.Status.Should().Be(StocktakeDeliveryStatus.Succeeded);
            result.Export!.File.Name.Should().StartWith("Stocktake-WH-OLD-SCANNER-OLD-");
        }
        finally
        {
            releaseSnapshot.TrySetResult();
            tempDirectory.Delete(recursive: true);
        }

        async Task<List<StocktakeItem>> ReadSnapshotAsync()
        {
            snapshotStarted.TrySetResult();
            await releaseSnapshot.Task;
            return [new StocktakeItem { Barcode = "ABC123", Qty = 1 }];
        }
    }
}
