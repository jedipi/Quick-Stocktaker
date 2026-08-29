using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeDeliveryWorkflowFailureTests
{
    [Fact]
    public async Task CreateExportAsync_WhenExportFails_ReturnsFailedAndLogsOnce()
    {
        var repository = Substitute.For<ISQLiteRepository<StocktakeItem>>();
        var failure = new IOException("private diagnostic");
        repository.GetAllAsync().Returns(Task.FromException<List<StocktakeItem>>(failure));
        var logger = new CapturingLogger<StocktakeDeliveryWorkflow>();
        var exporter = new CsvExportService(
            repository,
            new TestAppPreferences(),
            new TestAppFileSystem(Path.GetTempPath()));
        IStocktakeDeliveryWorkflow workflow = new StocktakeDeliveryWorkflow(exporter, logger);

        var result = await workflow.CreateExportAsync(TestContext.Current.CancellationToken);

        result.Status.Should().Be(StocktakeDeliveryStatus.Failed);
        result.Export.Should().BeNull();
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Error);
        logger.Entries[0].Exception.Should().BeSameAs(failure);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
