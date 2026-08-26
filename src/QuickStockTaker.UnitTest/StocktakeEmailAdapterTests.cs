using FluentAssertions;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeEmailAdapterTests
{
    [Fact]
    public async Task SendAsync_WhenAlreadyCancelled_DoesNotComposeOrSend()
    {
        var delivery = new StocktakeEmailDelivery(
            new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv"))),
            "recipient@example.com",
            "sender@example.com",
            new StocktakeEmailConfiguration("Other", "smtp.example.com", 587, "user", "password"),
            new StocktakeEmailContent("SCANNER-01", 42, "WH-A", "26/08/2026"));
        var sendCalled = false;
        var adapter = new StocktakeEmailAdapter((_, _, _) =>
        {
            sendCalled = true;
            return Task.CompletedTask;
        });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var action = () => adapter.SendAsync(delivery, cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        sendCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ComposesExistingMessageAndDelegatesImmutableDelivery()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var delivery = new StocktakeEmailDelivery(
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
                "26/08/2026"));
        StocktakeEmailMessage? capturedMessage = null;
        StocktakeEmailConfiguration? capturedConfiguration = null;
        CancellationToken capturedToken = default;
        var adapter = new StocktakeEmailAdapter((message, configuration, cancellationToken) =>
        {
            capturedMessage = message;
            capturedConfiguration = configuration;
            capturedToken = cancellationToken;
            return Task.CompletedTask;
        });

        await adapter.SendAsync(delivery, TestContext.Current.CancellationToken);

        capturedMessage.Should().Be(new StocktakeEmailMessage(
            "recipient@example.com",
            "sender@example.com",
            "[Quick Stocktaker] Data for stocktake 42, Site WH-A, Device ID SCANNER-01",
            string.Join(Environment.NewLine,
                "<html><body>",
                "The file <b>stocktake.csv</b> included in this email contains the stocktake data for <br><br>",
                "Scanner SCANNER-01<br>",
                "Stocktake number: 42<br>",
                "Site: WH-A<br>",
                "Stocktake Date:26/08/2026<br>",
                "</body></html>",
                string.Empty),
            export.File));
        capturedConfiguration.Should().Be(delivery.Configuration);
        capturedToken.Should().Be(TestContext.Current.CancellationToken);
    }
}
