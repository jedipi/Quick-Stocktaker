using Controls.UserDialogs.Maui;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public class FtpSetingViewModelTests
{
    [Fact]
    public async Task AppearingCommand_WhenConfigurationLoadFails_ExposesRetryUntilNextLoadSucceeds()
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var secureStorage = Substitute.For<ISecureStorageService>();
        secureStorage.GetAsync(Constants.FtpUsername)
            .Returns(
                _ => Task.FromException<string>(new InvalidOperationException("Secure storage failed.")),
                _ => Task.FromResult("stocktaker"));
        secureStorage.GetAsync(Constants.FtpPassword).Returns("secret");
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.FtpUseSftp, true);
        preferences.Set(Constants.FtpHost, "sftp.example.com");
        preferences.Set(Constants.FtpPort, "22");
        var viewModel = new FtpSetingViewModel(
            dialogs,
            Substitute.For<IStocktakeRemoteConnectionService>(),
            new StocktakeRemoteConfigurationGate(),
            preferences,
            secureStorage,
            Substitute.For<IPageDialogService>(),
            NullLogger<FtpSetingViewModel>.Instance);

        await viewModel.AppearingCommand.ExecuteAsync(null);

        viewModel.IsFtpConfigurationLoaded.Should().BeFalse();
        viewModel.HasFtpConfigurationLoadError.Should().BeTrue();
        await dialogs.Received(1).AlertAsync(
            "FTP/SFTP settings could not be loaded. Please try again.",
            "Error",
            "OK",
            "ic_error.png",
            Arg.Any<CancellationToken>());

        await viewModel.AppearingCommand.ExecuteAsync(null);

        viewModel.IsFtpConfigurationLoaded.Should().BeTrue();
        viewModel.HasFtpConfigurationLoadError.Should().BeFalse();
        viewModel.FtpUseSftp.Should().BeTrue();
        viewModel.FtpHost.Should().Be("sftp.example.com");
        viewModel.FtpPort.Should().Be("22");
    }

    [Fact]
    public async Task AppearingCommand_WhileConfigurationIsLoading_KeepsSettingsInteractionDisabled()
    {
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.FtpUseSftp, true);
        preferences.Set(Constants.FtpHost, "sftp.example.com");
        preferences.Set(Constants.FtpPort, "22");
        var usernameReadStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseUsernameRead = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var secureStorage = Substitute.For<ISecureStorageService>();
        secureStorage.GetAsync(Constants.FtpUsername).Returns(async _ =>
        {
            usernameReadStarted.TrySetResult();
            await releaseUsernameRead.Task;
            return "stocktaker";
        });
        secureStorage.GetAsync(Constants.FtpPassword).Returns("secret");
        var viewModel = new FtpSetingViewModel(
            Substitute.For<IUserDialogs>(),
            Substitute.For<IStocktakeRemoteConnectionService>(),
            new StocktakeRemoteConfigurationGate(),
            preferences,
            secureStorage,
            Substitute.For<IPageDialogService>(),
            NullLogger<FtpSetingViewModel>.Instance);

        var appearing = viewModel.AppearingCommand.ExecuteAsync(null);
        await usernameReadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        viewModel.IsFtpConfigurationLoaded.Should().BeFalse();
        releaseUsernameRead.TrySetResult();
        await appearing;
        viewModel.IsFtpConfigurationLoaded.Should().BeTrue();
        viewModel.FtpUseSftp.Should().BeTrue();
    }

    [Fact]
    public async Task UseSftpToggledCommand_WhenToggledTwiceWhileWaiting_PersistsLatestSelection()
    {
        var gate = new StocktakeRemoteConfigurationGate();
        var gateOccupied = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseGate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var occupyingOperation = gate.RunAsync(async () =>
        {
            gateOccupied.TrySetResult();
            await releaseGate.Task;
        }, TestContext.Current.CancellationToken);
        await gateOccupied.Task.WaitAsync(TestContext.Current.CancellationToken);
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.FtpUseSftp, false);
        preferences.Set(Constants.FtpPort, "21");
        var viewModel = new FtpSetingViewModel(
            Substitute.For<IUserDialogs>(),
            Substitute.For<IStocktakeRemoteConnectionService>(),
            gate,
            preferences,
            Substitute.For<ISecureStorageService>(),
            Substitute.For<IPageDialogService>(),
            NullLogger<FtpSetingViewModel>.Instance);
        var command = viewModel.UseSftpToggledCommand;

        if (command.CanExecute(true))
            command.Execute(true);
        var firstToggle = command.ExecutionTask!;
        if (command.CanExecute(false))
            command.Execute(false);
        var latestToggle = command.ExecutionTask!;
        releaseGate.TrySetResult();
        await Task.WhenAll(occupyingOperation, firstToggle, latestToggle);

        preferences.GetBool(Constants.FtpUseSftp, true).Should().BeFalse();
        preferences.GetString(Constants.FtpPort, "").Should().Be("21");
        viewModel.FtpUseSftp.Should().BeFalse();
        viewModel.FtpPort.Should().Be("21");
    }

    [Fact]
    public async Task UseSftpToggledCommand_WhenUsingPreviousDefaultPort_PersistsNewProtocolDefault()
    {
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.FtpUseSftp, true);
        preferences.Set(Constants.FtpPort, "22");
        var viewModel = new FtpSetingViewModel(
            Substitute.For<IUserDialogs>(),
            Substitute.For<IStocktakeRemoteConnectionService>(),
            new StocktakeRemoteConfigurationGate(),
            preferences,
            Substitute.For<ISecureStorageService>(),
            Substitute.For<IPageDialogService>(),
            NullLogger<FtpSetingViewModel>.Instance);

        await viewModel.UseSftpToggledCommand.ExecuteAsync(false);

        preferences.GetBool(Constants.FtpUseSftp, true).Should().BeFalse();
        preferences.GetString(Constants.FtpPort, "").Should().Be("21");
        viewModel.FtpUseSftp.Should().BeFalse();
        viewModel.FtpPort.Should().Be("21");
    }

    [Fact]
    public void GetPortForProtocolChange_WhenExistingPortIsPreviousDefault_UsesNewProtocolDefault()
    {
        var port = FtpSetingViewModel.GetPortForProtocolChange(
            previousUseSftp: true,
            nextUseSftp: false,
            currentPort: "22");

        port.Should().Be("21");
    }

    [Fact]
    public void GetPortForProtocolChange_WhenExistingPortIsCustom_KeepsCustomPort()
    {
        var port = FtpSetingViewModel.GetPortForProtocolChange(
            previousUseSftp: true,
            nextUseSftp: false,
            currentPort: "8022");

        port.Should().Be("8022");
    }
}
