using FluentAssertions;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeRemoteTransferAdapterTests
{
    [Fact]
    public async Task FtpAdapter_BuildsRemotePathAndDelegatesTransfer()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var configuration = new StocktakeRemoteConfiguration(
            StocktakeRemoteProtocol.Ftp,
            "ftp.example.com",
            21,
            "/exports/daily",
            "user",
            "password");
        FileInfo? capturedFile = null;
        StocktakeRemoteConfiguration? capturedConfiguration = null;
        string? capturedRemotePath = null;
        CancellationToken capturedToken = default;
        var adapter = new FtpStocktakeRemoteTransferAdapter((file, settings, remotePath, token) =>
        {
            capturedFile = file;
            capturedConfiguration = settings;
            capturedRemotePath = remotePath;
            capturedToken = token;
            return Task.CompletedTask;
        });

        await adapter.TransferAsync(export, configuration, TestContext.Current.CancellationToken);

        adapter.Protocol.Should().Be(StocktakeRemoteProtocol.Ftp);
        capturedFile.Should().Be(export.File);
        capturedConfiguration.Should().Be(configuration);
        capturedRemotePath.Should().Be("/exports/daily/stocktake.csv");
        capturedToken.Should().Be(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SftpAdapter_BuildsRemotePathAndDelegatesTransfer()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var configuration = new StocktakeRemoteConfiguration(
            StocktakeRemoteProtocol.Sftp,
            "sftp.example.com",
            22,
            "exports/daily",
            "user",
            "password");
        string? capturedRemotePath = null;
        var adapter = new SftpStocktakeRemoteTransferAdapter((_, _, remotePath, _) =>
            capturedRemotePath = remotePath);

        await adapter.TransferAsync(export, configuration, TestContext.Current.CancellationToken);

        adapter.Protocol.Should().Be(StocktakeRemoteProtocol.Sftp);
        capturedRemotePath.Should().Be("exports/daily/stocktake.csv");
    }

    [Fact]
    public async Task SftpAdapter_WhenAlreadyCancelled_DoesNotStartTransfer()
    {
        var export = new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv")));
        var configuration = new StocktakeRemoteConfiguration(
            StocktakeRemoteProtocol.Sftp,
            "sftp.example.com",
            22,
            string.Empty,
            "user",
            "password");
        var transferStarted = false;
        var adapter = new SftpStocktakeRemoteTransferAdapter((_, _, _, _) => transferStarted = true);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var action = () => adapter.TransferAsync(export, configuration, cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        transferStarted.Should().BeFalse();
    }
}
