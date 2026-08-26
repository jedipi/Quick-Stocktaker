using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeRemoteDeliveryWorkflowTests
{
    [Theory]
    [InlineData("", "21", "stocktaker", "secret", "FTP/SFTP host is not configured or not valid.")]
    [InlineData("ftp.example.com", "0", "stocktaker", "secret", "FTP/SFTP port is not configured or not valid.")]
    [InlineData("ftp.example.com", "21", "", "secret", "FTP/SFTP username is not configured.")]
    [InlineData("ftp.example.com", "21", "stocktaker", "", "FTP/SFTP password is not configured.")]
    public async Task DeliverToConfiguredRemoteAsync_WhenConfigurationIsInvalid_ReturnsValidationMessageWithoutCreatingExport(
        string host,
        string port,
        string username,
        string password,
        string expectedMessage)
    {
        var exporter = Substitute.For<ICsvExportService>();
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.FtpHost, host);
        preferences.Set(Constants.FtpPort, port);
        var secureStorage = Substitute.For<ISecureStorageService>();
        secureStorage.GetAsync(Constants.FtpUsername).Returns(username);
        secureStorage.GetAsync(Constants.FtpPassword).Returns(password);
        var adapter = Substitute.For<IStocktakeRemoteTransferAdapter>();
        var transferStarting = Substitute.For<Action>();
        var workflow = new StocktakeDeliveryWorkflow(
            exporter,
            NullLogger<StocktakeDeliveryWorkflow>.Instance,
            new StocktakeDeliveryOperationGate(),
            preferences,
            secureStorage,
            new StocktakeRemoteConfigurationValidator(),
            [adapter]);

        var result = await workflow.DeliverToConfiguredRemoteAsync(
            TestContext.Current.CancellationToken,
            transferStarting);

        result.Status.Should().Be(StocktakeDeliveryStatus.InvalidConfiguration);
        result.Message.Should().Be(expectedMessage);
        await exporter.DidNotReceive().CreateExportAsync(Arg.Any<CancellationToken>());
        await adapter.DidNotReceive().TransferAsync(
            Arg.Any<StocktakeExport>(),
            Arg.Any<StocktakeRemoteConfiguration>(),
            Arg.Any<CancellationToken>());
        transferStarting.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task DeliverToConfiguredRemoteAsync_WhenFtpIsConfigured_CreatesAndTransfersExportWithCapturedConfiguration()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.FtpUseSftp, false);
        preferences.Set(Constants.FtpHost, " ftp.example.com ");
        preferences.Set(Constants.FtpPort, "2121");
        preferences.Set(Constants.FtpFolder, "/exports/daily");
        var secureStorage = Substitute.For<ISecureStorageService>();
        secureStorage.GetAsync(Constants.FtpUsername).Returns("stocktaker");
        secureStorage.GetAsync(Constants.FtpPassword).Returns("secret");
        var adapter = Substitute.For<IStocktakeRemoteTransferAdapter>();
        adapter.Protocol.Returns(StocktakeRemoteProtocol.Ftp);
        var transferStarted = false;
        adapter.TransferAsync(export, Arg.Any<StocktakeRemoteConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                transferStarted.Should().BeTrue();
                return Task.CompletedTask;
            });
        var workflow = new StocktakeDeliveryWorkflow(
            exporter,
            NullLogger<StocktakeDeliveryWorkflow>.Instance,
            new StocktakeDeliveryOperationGate(),
            preferences,
            secureStorage,
            new StocktakeRemoteConfigurationValidator(),
            [adapter]);

        var result = await workflow.DeliverToConfiguredRemoteAsync(
            TestContext.Current.CancellationToken,
            () => transferStarted = true);

        result.Should().Be(StocktakeDeliveryResult.Succeeded(
            export,
            "Data uploaded successfully: stocktake.csv"));
        await adapter.Received(1).TransferAsync(
            export,
            new StocktakeRemoteConfiguration(
                StocktakeRemoteProtocol.Ftp,
                "ftp.example.com",
                2121,
                "/exports/daily",
                "stocktaker",
                "secret"),
            TestContext.Current.CancellationToken);
        transferStarted.Should().BeTrue();
    }

    [Fact]
    public async Task DeliverToConfiguredRemoteAsync_WhenTransferIsCancelled_ReturnsCancelled()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var preferences = CreateValidPreferences(useSftp: false);
        var secureStorage = CreateValidSecureStorage();
        var adapter = Substitute.For<IStocktakeRemoteTransferAdapter>();
        adapter.Protocol.Returns(StocktakeRemoteProtocol.Ftp);
        adapter.TransferAsync(export, Arg.Any<StocktakeRemoteConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled(call.Arg<CancellationToken>()));
        var workflow = new StocktakeDeliveryWorkflow(
            exporter,
            NullLogger<StocktakeDeliveryWorkflow>.Instance,
            new StocktakeDeliveryOperationGate(),
            preferences,
            secureStorage,
            new StocktakeRemoteConfigurationValidator(),
            [adapter]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await workflow.DeliverToConfiguredRemoteAsync(cancellation.Token);

        result.Status.Should().Be(StocktakeDeliveryStatus.Cancelled);
        result.Export.Should().BeNull();
    }

    [Fact]
    public async Task DeliverToConfiguredRemoteAsync_WhenCancellationDisposesSftpClient_ReturnsCancelledWithoutLogging()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var adapter = Substitute.For<IStocktakeRemoteTransferAdapter>();
        adapter.Protocol.Returns(StocktakeRemoteProtocol.Sftp);
        adapter.TransferAsync(export, Arg.Any<StocktakeRemoteConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ObjectDisposedException("SftpClient")));
        var logger = new CapturingLogger<StocktakeDeliveryWorkflow>();
        var workflow = new StocktakeDeliveryWorkflow(
            exporter,
            logger,
            new StocktakeDeliveryOperationGate(),
            CreateValidPreferences(useSftp: true),
            CreateValidSecureStorage(),
            new StocktakeRemoteConfigurationValidator(),
            [adapter]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await workflow.DeliverToConfiguredRemoteAsync(cancellation.Token);

        result.Status.Should().Be(StocktakeDeliveryStatus.Cancelled);
        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task DeliverToConfiguredRemoteAsync_WhenAdapterFails_ReturnsSafeFailureAndLogsOnce()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var failure = new IOException("server ftp.example.com rejected secret");
        var adapter = Substitute.For<IStocktakeRemoteTransferAdapter>();
        adapter.Protocol.Returns(StocktakeRemoteProtocol.Ftp);
        var transferStarting = Substitute.For<Action>();
        adapter.TransferAsync(export, Arg.Any<StocktakeRemoteConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(failure));
        var logger = new CapturingLogger<StocktakeDeliveryWorkflow>();
        var workflow = new StocktakeDeliveryWorkflow(
            exporter,
            logger,
            new StocktakeDeliveryOperationGate(),
            CreateValidPreferences(useSftp: false),
            CreateValidSecureStorage(),
            new StocktakeRemoteConfigurationValidator(),
            [adapter]);

        var result = await workflow.DeliverToConfiguredRemoteAsync(
            TestContext.Current.CancellationToken,
            transferStarting);

        result.Should().Be(StocktakeDeliveryResult.Failed("Data upload failed. Please try again."));
        result.Message.Should().NotContain("ftp.example.com").And.NotContain("secret");
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Should().Be((LogLevel.Error, failure));
        transferStarting.Received(1).Invoke();
    }

    [Fact]
    public async Task DeliverToConfiguredRemoteAsync_WhenConfigurationChangesDuringTransfer_UsesChangesOnlyOnNextOperation()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var preferences = CreateValidPreferences(useSftp: false);
        preferences.Set(Constants.FtpHost, "ftp.example.com");
        var secureStorage = Substitute.For<ISecureStorageService>();
        secureStorage.GetAsync(Constants.FtpUsername).Returns("first-user", "second-user");
        secureStorage.GetAsync(Constants.FtpPassword).Returns("first-password", "second-password");
        var ftpAdapter = Substitute.For<IStocktakeRemoteTransferAdapter>();
        ftpAdapter.Protocol.Returns(StocktakeRemoteProtocol.Ftp);
        ftpAdapter.TransferAsync(export, Arg.Any<StocktakeRemoteConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                preferences.Set(Constants.FtpUseSftp, true);
                preferences.Set(Constants.FtpHost, "sftp.example.com");
                preferences.Set(Constants.FtpPort, "22");
                return Task.CompletedTask;
            });
        var sftpAdapter = Substitute.For<IStocktakeRemoteTransferAdapter>();
        sftpAdapter.Protocol.Returns(StocktakeRemoteProtocol.Sftp);
        var workflow = new StocktakeDeliveryWorkflow(
            exporter,
            NullLogger<StocktakeDeliveryWorkflow>.Instance,
            new StocktakeDeliveryOperationGate(),
            preferences,
            secureStorage,
            new StocktakeRemoteConfigurationValidator(),
            [ftpAdapter, sftpAdapter]);

        await workflow.DeliverToConfiguredRemoteAsync(TestContext.Current.CancellationToken);
        await workflow.DeliverToConfiguredRemoteAsync(TestContext.Current.CancellationToken);

        await ftpAdapter.Received(1).TransferAsync(
            export,
            Arg.Is<StocktakeRemoteConfiguration>(configuration =>
                configuration.Protocol == StocktakeRemoteProtocol.Ftp &&
                configuration.Host == "ftp.example.com" &&
                configuration.Username == "first-user" &&
                configuration.Password == "first-password"),
            TestContext.Current.CancellationToken);
        await sftpAdapter.Received(1).TransferAsync(
            export,
            Arg.Is<StocktakeRemoteConfiguration>(configuration =>
                configuration.Protocol == StocktakeRemoteProtocol.Sftp &&
                configuration.Host == "sftp.example.com" &&
                configuration.Username == "second-user" &&
                configuration.Password == "second-password"),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeliverToConfiguredRemoteAsync_WhenStocktakeHasNoData_ReturnsNoStocktakeDataWithoutTransfer()
    {
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs((StocktakeExport)null!);
        var adapter = Substitute.For<IStocktakeRemoteTransferAdapter>();
        adapter.Protocol.Returns(StocktakeRemoteProtocol.Ftp);
        var transferStarting = Substitute.For<Action>();
        var workflow = new StocktakeDeliveryWorkflow(
            exporter,
            NullLogger<StocktakeDeliveryWorkflow>.Instance,
            new StocktakeDeliveryOperationGate(),
            CreateValidPreferences(useSftp: false),
            CreateValidSecureStorage(),
            new StocktakeRemoteConfigurationValidator(),
            [adapter]);

        var result = await workflow.DeliverToConfiguredRemoteAsync(
            TestContext.Current.CancellationToken,
            transferStarting);

        result.Status.Should().Be(StocktakeDeliveryStatus.NoStocktakeData);
        await adapter.DidNotReceive().TransferAsync(
            Arg.Any<StocktakeExport>(),
            Arg.Any<StocktakeRemoteConfiguration>(),
            Arg.Any<CancellationToken>());
        transferStarting.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task DeliverToConfiguredRemoteAsync_WhenAnotherWorkflowIsTransferring_ReturnsAlreadyInProgressImmediately()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var transferStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseTransfer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var adapter = Substitute.For<IStocktakeRemoteTransferAdapter>();
        adapter.Protocol.Returns(StocktakeRemoteProtocol.Ftp);
        adapter.TransferAsync(export, Arg.Any<StocktakeRemoteConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                transferStarted.TrySetResult();
                await releaseTransfer.Task;
            });
        var gate = new StocktakeDeliveryOperationGate();
        var preferences = CreateValidPreferences(useSftp: false);
        var secureStorage = CreateValidSecureStorage();
        var firstWorkflow = new StocktakeDeliveryWorkflow(
            exporter,
            NullLogger<StocktakeDeliveryWorkflow>.Instance,
            gate,
            preferences,
            secureStorage,
            new StocktakeRemoteConfigurationValidator(),
            [adapter]);
        var secondWorkflow = new StocktakeDeliveryWorkflow(
            exporter,
            NullLogger<StocktakeDeliveryWorkflow>.Instance,
            gate,
            preferences,
            secureStorage,
            new StocktakeRemoteConfigurationValidator(),
            [adapter]);

        var firstOperation = firstWorkflow.DeliverToConfiguredRemoteAsync(TestContext.Current.CancellationToken);
        await transferStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var secondResult = await secondWorkflow.DeliverToConfiguredRemoteAsync(TestContext.Current.CancellationToken);
        releaseTransfer.TrySetResult();
        var firstResult = await firstOperation;

        secondResult.Status.Should().Be(StocktakeDeliveryStatus.AlreadyInProgress);
        firstResult.Status.Should().Be(StocktakeDeliveryStatus.Succeeded);
    }

    private static TestAppPreferences CreateValidPreferences(bool useSftp)
    {
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.FtpUseSftp, useSftp);
        preferences.Set(Constants.FtpHost, "files.example.com");
        preferences.Set(Constants.FtpPort, useSftp ? "22" : "21");
        return preferences;
    }

    private static ISecureStorageService CreateValidSecureStorage()
    {
        var secureStorage = Substitute.For<ISecureStorageService>();
        secureStorage.GetAsync(Constants.FtpUsername).Returns("stocktaker");
        secureStorage.GetAsync(Constants.FtpPassword).Returns("secret");
        return secureStorage;
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
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, exception));

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
