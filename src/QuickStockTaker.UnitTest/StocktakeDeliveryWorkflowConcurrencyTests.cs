using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeDeliveryWorkflowConcurrencyTests
{
    [Fact]
    public async Task CreateExportAsync_WhenAnotherWorkflowInstanceIsRunning_ReturnsAlreadyInProgressImmediately()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-delivery-concurrent-");
        var snapshotStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSnapshot = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var repository = Substitute.For<ISQLiteRepository<StocktakeItem>>();
            repository.GetAllAsync().Returns(_ => ReadSnapshotAsync());
            var exporter = new CsvExportService(
                repository,
                new TestAppPreferences(),
                new TestAppFileSystem(tempDirectory.FullName));
            var operationGate = new StocktakeDeliveryOperationGate();
            IStocktakeDeliveryWorkflow firstWorkflow = new StocktakeDeliveryWorkflow(
                exporter,
                NullLogger<StocktakeDeliveryWorkflow>.Instance,
                operationGate);
            IStocktakeDeliveryWorkflow secondWorkflow = new StocktakeDeliveryWorkflow(
                exporter,
                NullLogger<StocktakeDeliveryWorkflow>.Instance,
                operationGate);

            var firstOperation = firstWorkflow.CreateExportAsync(TestContext.Current.CancellationToken);
            await snapshotStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            var secondOperation = secondWorkflow.CreateExportAsync(TestContext.Current.CancellationToken);
            var completed = await Task.WhenAny(secondOperation, Task.Delay(250, TestContext.Current.CancellationToken));

            releaseSnapshot.TrySetResult();
            var firstResult = await firstOperation;
            var secondResult = await secondOperation;

            completed.Should().BeSameAs(secondOperation);
            secondResult.Status.Should().Be(StocktakeDeliveryStatus.AlreadyInProgress);
            firstResult.Status.Should().Be(StocktakeDeliveryStatus.Succeeded);
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
