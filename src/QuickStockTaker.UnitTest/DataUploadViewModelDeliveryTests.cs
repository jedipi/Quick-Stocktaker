using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public sealed class DataUploadViewModelDeliveryTests
{
    [Fact]
    public async Task EmailCommand_WhenRecipientIsInvalid_ShowsExistingValidationAndDoesNotDeliver()
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var pageDialogs = Substitute.For<IPageDialogService>();
        pageDialogs.DisplayPromptAsync(
                "Email Stocktake Data",
                "Please type in your email address:",
                "OK")
            .ReturnsForAnyArgs("not-an-email");
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        var viewModel = CreateViewModel(
            dialogs,
            workflow,
            Substitute.For<ICsvExportService>(),
            pageDialogs);

        await viewModel.EmailCommand.ExecuteAsync(null);

        await dialogs.Received(1).AlertAsync(
            "A valid email address is required.",
            "Error",
            "OK",
            "ic_error.png",
            Arg.Any<CancellationToken>());
        await workflow.DidNotReceive().DeliverByEmailAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<Action>());
    }

    [Fact]
    public async Task EmailCommand_WhenWorkflowSucceeds_PreservesPromptProgressAndSuccessAlert()
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var progress = Substitute.For<IHudDialog>();
        dialogs.Progress("Emailing data", "Cancel", true, null, Arg.Any<Action>()).Returns(progress);
        var pageDialogs = Substitute.For<IPageDialogService>();
        pageDialogs.DisplayPromptAsync(
                "Email Stocktake Data",
                "Please type in your email address:",
                "OK")
            .ReturnsForAnyArgs(" recipient@example.com ");
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        workflow.DeliverByEmailAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action>())
            .Returns(call =>
            {
                call.Arg<Action>()();
                return StocktakeDeliveryResult.Succeeded(
                    new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv"))),
                    "Data send successfully.");
            });
        var viewModel = CreateViewModel(
            dialogs,
            workflow,
            Substitute.For<ICsvExportService>(),
            pageDialogs);

        await viewModel.EmailCommand.ExecuteAsync(null);

        await workflow.Received(1).DeliverByEmailAsync(
            "recipient@example.com",
            Arg.Is<CancellationToken>(token => token.CanBeCanceled),
            Arg.Any<Action>());
        progress.Received(1).Show();
        await dialogs.Received(1).AlertAsync(
            "Data send successfully.",
            null,
            null,
            null,
            Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            progress.Show();
            progress.Dispose();
            _ = dialogs.AlertAsync(
                "Data send successfully.",
                null,
                null,
                null,
                Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task EmailCommand_WhenWorkflowHasNoStocktakeData_PreservesExistingExportError()
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var pageDialogs = CreateEmailPrompt("recipient@example.com");
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        workflow.DeliverByEmailAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action>())
            .Returns(StocktakeDeliveryResult.NoStocktakeData());
        var viewModel = CreateViewModel(
            dialogs,
            workflow,
            Substitute.For<ICsvExportService>(),
            pageDialogs);

        await viewModel.EmailCommand.ExecuteAsync(null);

        await dialogs.Received(1).AlertAsync(
            "Data export fail. Please try again.",
            "Error",
            "OK",
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmailCommand_WhenConfigurationIsInvalid_ShowsSafeTypedMessage()
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var pageDialogs = CreateEmailPrompt("recipient@example.com");
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        workflow.DeliverByEmailAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action>())
            .Returns(StocktakeDeliveryResult.InvalidConfiguration(
                "SMTP host is not configured or not valid."));
        var viewModel = CreateViewModel(
            dialogs,
            workflow,
            Substitute.For<ICsvExportService>(),
            pageDialogs);

        await viewModel.EmailCommand.ExecuteAsync(null);

        await dialogs.Received(1).AlertAsync(
            "SMTP host is not configured or not valid.",
            "ERROR",
            "OK",
            null,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(StocktakeDeliveryStatus.Cancelled, null, "Email cancelled.")]
    [InlineData(StocktakeDeliveryStatus.AlreadyInProgress, null, "Another stocktake delivery is already in progress.")]
    [InlineData(StocktakeDeliveryStatus.Failed, "Data send fail.", "Data send fail.")]
    public async Task EmailCommand_WhenWorkflowDoesNotSucceed_MapsTypedOutcomeToSafeAlert(
        StocktakeDeliveryStatus status,
        string? resultMessage,
        string expectedMessage)
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var pageDialogs = CreateEmailPrompt("recipient@example.com");
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        workflow.DeliverByEmailAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action>())
            .Returns(new StocktakeDeliveryResult(status, Message: resultMessage));
        var viewModel = CreateViewModel(
            dialogs,
            workflow,
            Substitute.For<ICsvExportService>(),
            pageDialogs);

        await viewModel.EmailCommand.ExecuteAsync(null);

        await dialogs.Received(1).AlertAsync(
            expectedMessage,
            null,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CsvCommand_WhenWorkflowHasNoStocktakeData_ShowsExistingErrorAlert()
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        workflow.CreateExportAsync(TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(StocktakeDeliveryResult.NoStocktakeData());
        var csvExport = Substitute.For<ICsvExportService>();
        var viewModel = CreateViewModel(dialogs, workflow, csvExport);

        await viewModel.CsvCommand.ExecuteAsync(null);

        await workflow.ReceivedWithAnyArgs(1).CreateExportAsync(TestContext.Current.CancellationToken);
        await dialogs.ReceivedWithAnyArgs(1).AlertAsync(
            "No data is exported. Please try again.",
            "Error",
            "OK",
            "ic_error.png",
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CsvCommand_WhenWorkflowCreatesExport_ShowsExistingShareAndSaveActions()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-viewmodel-export-");
        try
        {
            var file = new FileInfo(Path.Combine(tempDirectory.FullName, "Stocktake-WH-A-SCANNER-01.csv"));
            await File.WriteAllTextAsync(file.FullName, "Barcode,Qty", TestContext.Current.CancellationToken);
            var dialogs = Substitute.For<IUserDialogs>();
            var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
            workflow.CreateExportAsync(TestContext.Current.CancellationToken)
                .ReturnsForAnyArgs(StocktakeDeliveryResult.Succeeded(new StocktakeExport(file)));
            var csvExport = Substitute.For<ICsvExportService>();
            var viewModel = CreateViewModel(dialogs, workflow, csvExport);

            await viewModel.CsvCommand.ExecuteAsync(null);

            dialogs.Received(1).ActionSheet(Arg.Is<ActionSheetConfig>(config =>
                config.Title == "CSV File" &&
                config.Message == "Data exported: Stocktake-WH-A-SCANNER-01.csv" &&
                config.Options.Select(option => option.Text).SequenceEqual(new[] { "Share", "Save" })));
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task FtpCommand_WhenWorkflowSucceeds_ShowsExistingSuccessPresentation()
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var progress = Substitute.For<IHudDialog>();
        dialogs.Progress("Uploading data", "Cancel", true, null, Arg.Any<Action>()).Returns(progress);
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        workflow.DeliverToConfiguredRemoteAsync(Arg.Any<CancellationToken>(), Arg.Any<Action>())
            .Returns(call =>
            {
                call.Arg<Action>()();
                return StocktakeDeliveryResult.Succeeded(
                    new StocktakeExport(new FileInfo(Path.Combine(Path.GetTempPath(), "stocktake.csv"))),
                    "Data uploaded successfully: stocktake.csv");
            });
        var viewModel = CreateViewModel(dialogs, workflow, Substitute.For<ICsvExportService>());

        await viewModel.FTPCommand.ExecuteAsync(null);

        await workflow.Received(1).DeliverToConfiguredRemoteAsync(
            Arg.Is<CancellationToken>(token => token.CanBeCanceled),
            Arg.Any<Action>());
        progress.Received(1).Show();
        await dialogs.Received(1).AlertAsync(
            "Data uploaded successfully: stocktake.csv",
            "Success",
            "OK",
            "ic_greentick.png",
            Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            progress.Show();
            progress.Dispose();
            _ = dialogs.AlertAsync(
                "Data uploaded successfully: stocktake.csv",
                "Success",
                "OK",
                "ic_greentick.png",
                Arg.Any<CancellationToken>());
        });
    }

    [Theory]
    [InlineData(StocktakeDeliveryStatus.InvalidConfiguration, "FTP/SFTP host is not configured or not valid.", "FTP/SFTP host is not configured or not valid.")]
    [InlineData(StocktakeDeliveryStatus.Cancelled, null, "Data upload cancelled.")]
    [InlineData(StocktakeDeliveryStatus.AlreadyInProgress, null, "Another stocktake delivery is already in progress.")]
    [InlineData(StocktakeDeliveryStatus.Failed, "Data upload failed. Please try again.", "Data upload failed. Please try again.")]
    public async Task FtpCommand_WhenWorkflowDoesNotSucceed_ShowsExistingErrorPresentation(
        StocktakeDeliveryStatus status,
        string? resultMessage,
        string expectedMessage)
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var progress = Substitute.For<IHudDialog>();
        dialogs.Progress("Uploading data", "Cancel", true, null, Arg.Any<Action>())
            .Returns(progress);
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        workflow.DeliverToConfiguredRemoteAsync(Arg.Any<CancellationToken>(), Arg.Any<Action>())
            .Returns(new StocktakeDeliveryResult(status, Message: resultMessage));
        var viewModel = CreateViewModel(dialogs, workflow, Substitute.For<ICsvExportService>());

        await viewModel.FTPCommand.ExecuteAsync(null);

        progress.DidNotReceive().Show();
        await dialogs.Received(1).AlertAsync(
            expectedMessage,
            "Error",
            "OK",
            "ic_error.png",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FtpCommand_WhenWorkflowHasNoStocktakeData_PreservesExistingExportErrorPresentation()
    {
        var dialogs = Substitute.For<IUserDialogs>();
        var progress = Substitute.For<IHudDialog>();
        dialogs.Progress("Uploading data", "Cancel", true, null, Arg.Any<Action>()).Returns(progress);
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        workflow.DeliverToConfiguredRemoteAsync(Arg.Any<CancellationToken>(), Arg.Any<Action>())
            .Returns(StocktakeDeliveryResult.NoStocktakeData());
        var viewModel = CreateViewModel(dialogs, workflow, Substitute.For<ICsvExportService>());

        await viewModel.FTPCommand.ExecuteAsync(null);

        progress.DidNotReceive().Show();
        await dialogs.Received(1).AlertAsync(
            "Data export fail. Please try again.",
            "Error",
            "OK",
            null,
            Arg.Any<CancellationToken>());
    }

    private static DataUploadViewModel CreateViewModel(
        IUserDialogs dialogs,
        IStocktakeDeliveryWorkflow workflow,
        ICsvExportService csvExport,
        IPageDialogService? pageDialogs = null)
    {
        var fileSystem = Substitute.For<IAppFileSystem>();
        fileSystem.GetDownloadFilePath(Arg.Any<string>())
            .Returns(call => Path.Combine(Path.GetTempPath(), call.Arg<string>()));

        return new DataUploadViewModel(
            dialogs,
            workflow,
            Substitute.For<IEmailUploadService>(),
            new EmailValidator(),
            Substitute.For<ISmtpService>(),
            new DataExportFactory(csvExport),
            Substitute.For<IAppPreferences>(),
            Substitute.For<ISecureStorageService>(),
            fileSystem,
            pageDialogs ?? Substitute.For<IPageDialogService>(),
            Substitute.For<ILogger<DataUploadViewModel>>());
    }

    private static IPageDialogService CreateEmailPrompt(string recipient)
    {
        var pageDialogs = Substitute.For<IPageDialogService>();
        pageDialogs.DisplayPromptAsync(
                "Email Stocktake Data",
                "Please type in your email address:",
                "OK")
            .ReturnsForAnyArgs(recipient);
        return pageDialogs;
    }
}
