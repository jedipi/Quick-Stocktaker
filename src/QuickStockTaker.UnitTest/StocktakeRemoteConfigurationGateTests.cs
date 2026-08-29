using FluentAssertions;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeRemoteConfigurationGateTests
{
    [Fact]
    public async Task RunAsync_WhenWaitingOperationIsCancelled_StopsWaiting()
    {
        var gate = new StocktakeRemoteConfigurationGate();
        var firstOperationStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstOperation = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var firstOperation = gate.RunAsync(async () =>
        {
            firstOperationStarted.TrySetResult();
            await releaseFirstOperation.Task;
        }, TestContext.Current.CancellationToken);
        await firstOperationStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var cancelledOperation = gate.RunAsync(
            () => Task.CompletedTask,
            cancellation.Token);
        cancellation.Cancel();

        try
        {
            var observeCancellation = async () => await cancelledOperation.WaitAsync(
                TimeSpan.FromSeconds(1),
                TestContext.Current.CancellationToken);
            await observeCancellation.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            releaseFirstOperation.TrySetResult();
            await firstOperation;
        }
    }
}
