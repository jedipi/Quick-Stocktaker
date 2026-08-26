using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeEmailDeliveryWorkflowTests
{
    [Theory]
    [InlineData("", "587", "smtp-user", "smtp-password", "sender@example.com", "SMTP host is not configured or not valid.")]
    [InlineData("smtp.example.com", "0", "smtp-user", "smtp-password", "sender@example.com", "SMTP port is not configured or not valid.")]
    [InlineData("smtp.example.com", "587", "", "smtp-password", "sender@example.com", "SMTP username is not configured.")]
    [InlineData("smtp.example.com", "587", "smtp-user", "", "sender@example.com", "SMTP password is not configured.")]
    [InlineData("smtp.example.com", "587", "smtp-user", "smtp-password", "", "SMTP sender is not configured or not valid.")]
    public async Task DeliverByEmailAsync_WhenConfigurationIsInvalid_ReturnsValidationWithoutCreatingExport(
        string host,
        string port,
        string username,
        string password,
        string sender,
        string expectedMessage)
    {
        var exporter = Substitute.For<ICsvExportService>();
        var adapter = Substitute.For<IStocktakeEmailAdapter>();
        var preferences = CreatePreferences();
        var workflow = CreateWorkflow(
            exporter,
            preferences,
            CreateSecureStorage(host, port, username, password, sender),
            adapter);

        var result = await workflow.DeliverByEmailAsync(
            "recipient@example.com",
            TestContext.Current.CancellationToken);

        result.Should().Be(StocktakeDeliveryResult.InvalidConfiguration(expectedMessage));
        await exporter.DidNotReceive().CreateExportAsync(Arg.Any<CancellationToken>());
        await adapter.DidNotReceive().SendAsync(
            Arg.Any<StocktakeEmailDelivery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeliverByEmailAsync_WhenStocktakeMetadataChangesDuringExport_UsesCapturedContent()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.SmtpProvider, "Other");
        preferences.Set(Constants.DeviceId, "SCANNER-OLD");
        preferences.Set(Constants.StocktakeNumber, 41);
        preferences.Set(Constants.Site, "WH-OLD");
        preferences.Set(Constants.StocktakeDate, new DateTime(2026, 8, 25));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(_ =>
        {
            preferences.Set(Constants.DeviceId, "SCANNER-NEW");
            preferences.Set(Constants.StocktakeNumber, 42);
            preferences.Set(Constants.Site, "WH-NEW");
            preferences.Set(Constants.StocktakeDate, new DateTime(2026, 8, 26));
            return export;
        });
        var adapter = Substitute.For<IStocktakeEmailAdapter>();
        var workflow = new StocktakeDeliveryWorkflow(
            exporter,
            NullLogger<StocktakeDeliveryWorkflow>.Instance,
            new StocktakeDeliveryOperationGate(),
            preferences,
            CreateSecureStorage(),
            new StocktakeRemoteConfigurationValidator(),
            [],
            new StocktakeEmailConfigurationValidator(),
            adapter);

        await workflow.DeliverByEmailAsync(
            "recipient@example.com",
            TestContext.Current.CancellationToken);

        await adapter.Received(1).SendAsync(
            Arg.Is<StocktakeEmailDelivery>(delivery =>
                delivery.Content == new StocktakeEmailContent(
                    "SCANNER-OLD",
                    41,
                    "WH-OLD",
                    new DateTime(2026, 8, 25).ToShortDateString())),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeliverByEmailAsync_WhenEmailIsConfigured_CreatesAndSendsExportWithCapturedIntent()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exportCreated = false;
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(_ =>
        {
            exportCreated = true;
            return export;
        });
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.SmtpProvider, "Other");
        preferences.Set(Constants.DeviceId, "SCANNER-01");
        preferences.Set(Constants.StocktakeNumber, 42);
        preferences.Set(Constants.Site, "WH-A");
        preferences.Set(Constants.StocktakeDate, new DateTime(2026, 8, 26));
        var secureStorage = CreateSecureStorage();
        var adapter = Substitute.For<IStocktakeEmailAdapter>();
        var deliveryStarted = false;
        adapter.SendAsync(Arg.Any<StocktakeEmailDelivery>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                exportCreated.Should().BeTrue();
                deliveryStarted.Should().BeTrue();
                return Task.CompletedTask;
            });
        var workflow = new StocktakeDeliveryWorkflow(
            exporter,
            NullLogger<StocktakeDeliveryWorkflow>.Instance,
            new StocktakeDeliveryOperationGate(),
            preferences,
            secureStorage,
            new StocktakeRemoteConfigurationValidator(),
            [],
            new StocktakeEmailConfigurationValidator(),
            adapter);

        var result = await workflow.DeliverByEmailAsync(
            "recipient@example.com",
            TestContext.Current.CancellationToken,
            () => deliveryStarted = true);

        result.Should().Be(StocktakeDeliveryResult.Succeeded(export, "Data send successfully."));
        await adapter.Received(1).SendAsync(
            new StocktakeEmailDelivery(
                export,
                "recipient@example.com",
                "sender@example.com",
                new StocktakeEmailConfiguration(
                    "Other",
                    "smtp.example.com",
                    587,
                    "smtp-user",
                    "smtp-password"),
                new StocktakeEmailContent(
                    "SCANNER-01",
                    42,
                    "WH-A",
                    new DateTime(2026, 8, 26).ToShortDateString())),
            TestContext.Current.CancellationToken);
        await secureStorage.Received(1).GetAsync(Constants.SmtpFrom);
        await secureStorage.Received(1).GetAsync(Constants.SmtpHost);
        await secureStorage.Received(1).GetAsync(Constants.SmtpPort);
        await secureStorage.Received(1).GetAsync(Constants.SmtpUsername);
        await secureStorage.Received(1).GetAsync(Constants.SmtpPassword);
    }

    [Fact]
    public async Task DeliverByEmailAsync_WhenStocktakeHasNoData_ReturnsNoStocktakeDataWithoutSending()
    {
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs((StocktakeExport)null!);
        var adapter = Substitute.For<IStocktakeEmailAdapter>();
        var deliveryStarting = Substitute.For<Action>();
        var workflow = CreateWorkflow(exporter, CreatePreferences(), CreateSecureStorage(), adapter);

        var result = await workflow.DeliverByEmailAsync(
            "recipient@example.com",
            TestContext.Current.CancellationToken,
            deliveryStarting);

        result.Status.Should().Be(StocktakeDeliveryStatus.NoStocktakeData);
        await adapter.DidNotReceive().SendAsync(
            Arg.Any<StocktakeEmailDelivery>(),
            Arg.Any<CancellationToken>());
        deliveryStarting.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task DeliverByEmailAsync_WhenSendIsCancelled_ReturnsCancelledWithoutLogging()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var adapter = Substitute.For<IStocktakeEmailAdapter>();
        adapter.SendAsync(Arg.Any<StocktakeEmailDelivery>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled(call.Arg<CancellationToken>()));
        var logger = new CapturingLogger<StocktakeDeliveryWorkflow>();
        var workflow = CreateWorkflow(
            exporter,
            CreatePreferences(),
            CreateSecureStorage(),
            adapter,
            logger);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await workflow.DeliverByEmailAsync(
            "recipient@example.com",
            cancellation.Token);

        result.Status.Should().Be(StocktakeDeliveryStatus.Cancelled);
        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task DeliverByEmailAsync_WhenAdapterFails_ReturnsSafeFailureLogsOnceAndDoesNotRetry()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var failure = new IOException("smtp.example.com rejected smtp-password");
        var adapter = Substitute.For<IStocktakeEmailAdapter>();
        adapter.SendAsync(Arg.Any<StocktakeEmailDelivery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(failure));
        var logger = new CapturingLogger<StocktakeDeliveryWorkflow>();
        var workflow = CreateWorkflow(
            exporter,
            CreatePreferences(),
            CreateSecureStorage(),
            adapter,
            logger);

        var result = await workflow.DeliverByEmailAsync(
            "recipient@example.com",
            TestContext.Current.CancellationToken);

        result.Should().Be(StocktakeDeliveryResult.Failed("Data send fail."));
        result.Message.Should().NotContain("smtp.example.com").And.NotContain("smtp-password");
        logger.Entries.Should().ContainSingle().Which.Should().Be((LogLevel.Error, failure));
        await adapter.Received(1).SendAsync(
            Arg.Any<StocktakeEmailDelivery>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeliverByEmailAsync_WhenConfigurationChangesDuringSend_UsesChangesOnlyOnNextOperation()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var preferences = CreatePreferences();
        var secureStorage = Substitute.For<ISecureStorageService>();
        secureStorage.GetAsync(Constants.SmtpFrom).Returns("first-sender@example.com", "ignored@example.com");
        secureStorage.GetAsync(Constants.SmtpHost).Returns("first.smtp.example.com", "second.smtp.example.com");
        secureStorage.GetAsync(Constants.SmtpPort).Returns("587", "465");
        secureStorage.GetAsync(Constants.SmtpUsername).Returns("first-user", "second-user");
        secureStorage.GetAsync(Constants.SmtpPassword).Returns("first-password", "second-password");
        var deliveries = new List<StocktakeEmailDelivery>();
        var adapter = Substitute.For<IStocktakeEmailAdapter>();
        adapter.SendAsync(Arg.Any<StocktakeEmailDelivery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                deliveries.Add(call.Arg<StocktakeEmailDelivery>());
                preferences.Set(Constants.SmtpProvider, "Gmail");
                return Task.CompletedTask;
            });
        var workflow = CreateWorkflow(exporter, preferences, secureStorage, adapter);

        await workflow.DeliverByEmailAsync("recipient@example.com", TestContext.Current.CancellationToken);
        await workflow.DeliverByEmailAsync("recipient@example.com", TestContext.Current.CancellationToken);

        deliveries.Should().HaveCount(2);
        deliveries[0].Sender.Should().Be("first-sender@example.com");
        deliveries[0].Configuration.Should().Be(new StocktakeEmailConfiguration(
            "Other",
            "first.smtp.example.com",
            587,
            "first-user",
            "first-password"));
        deliveries[1].Sender.Should().Be("recipient@example.com");
        deliveries[1].Configuration.Should().Be(new StocktakeEmailConfiguration(
            "Gmail",
            "second.smtp.example.com",
            465,
            "second-user",
            "second-password"));
    }

    [Fact]
    public async Task DeliverByEmailAsync_WhenAnotherDeliveryIsSending_ReturnsAlreadyInProgressImmediately()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var exporter = Substitute.For<ICsvExportService>();
        exporter.CreateExportAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(export);
        var sendStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var adapter = Substitute.For<IStocktakeEmailAdapter>();
        adapter.SendAsync(Arg.Any<StocktakeEmailDelivery>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                sendStarted.TrySetResult();
                await releaseSend.Task;
            });
        var gate = new StocktakeDeliveryOperationGate();
        var preferences = CreatePreferences();
        var secureStorage = CreateSecureStorage();
        var firstWorkflow = CreateWorkflow(exporter, preferences, secureStorage, adapter, operationGate: gate);
        var secondWorkflow = CreateWorkflow(exporter, preferences, secureStorage, adapter, operationGate: gate);

        var firstOperation = firstWorkflow.DeliverByEmailAsync(
            "recipient@example.com",
            TestContext.Current.CancellationToken);
        await sendStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        var secondResult = await secondWorkflow.DeliverByEmailAsync(
            "recipient@example.com",
            TestContext.Current.CancellationToken);
        releaseSend.TrySetResult();
        var firstResult = await firstOperation;

        secondResult.Status.Should().Be(StocktakeDeliveryStatus.AlreadyInProgress);
        firstResult.Status.Should().Be(StocktakeDeliveryStatus.Succeeded);
    }

    private static TestAppPreferences CreatePreferences()
    {
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.SmtpProvider, "Other");
        return preferences;
    }

    private static StocktakeDeliveryWorkflow CreateWorkflow(
        ICsvExportService exporter,
        IAppPreferences preferences,
        ISecureStorageService secureStorage,
        IStocktakeEmailAdapter adapter,
        ILogger<StocktakeDeliveryWorkflow>? logger = null,
        StocktakeDeliveryOperationGate? operationGate = null) =>
        new(
            exporter,
            logger ?? NullLogger<StocktakeDeliveryWorkflow>.Instance,
            operationGate ?? new StocktakeDeliveryOperationGate(),
            preferences,
            secureStorage,
            new StocktakeRemoteConfigurationValidator(),
            [],
            new StocktakeEmailConfigurationValidator(),
            adapter);

    private static ISecureStorageService CreateSecureStorage(
        string host = "smtp.example.com",
        string port = "587",
        string username = "smtp-user",
        string password = "smtp-password",
        string sender = "sender@example.com")
    {
        var secureStorage = Substitute.For<ISecureStorageService>();
        secureStorage.GetAsync(Constants.SmtpHost).Returns(host);
        secureStorage.GetAsync(Constants.SmtpPort).Returns(port);
        secureStorage.GetAsync(Constants.SmtpUsername).Returns(username);
        secureStorage.GetAsync(Constants.SmtpPassword).Returns(password);
        secureStorage.GetAsync(Constants.SmtpFrom).Returns(sender);
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
