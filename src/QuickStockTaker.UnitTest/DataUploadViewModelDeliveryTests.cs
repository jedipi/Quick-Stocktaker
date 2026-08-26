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

    private static DataUploadViewModel CreateViewModel(
        IUserDialogs dialogs,
        IStocktakeDeliveryWorkflow workflow,
        ICsvExportService csvExport)
    {
        var fileSystem = Substitute.For<IAppFileSystem>();
        fileSystem.GetDownloadFilePath(Arg.Any<string>())
            .Returns(call => Path.Combine(Path.GetTempPath(), call.Arg<string>()));

        return new DataUploadViewModel(
            dialogs,
            workflow,
            Substitute.For<IEmailUploadService>(),
            Substitute.For<IFtpUplodService>(),
            new EmailValidator(),
            Substitute.For<ISmtpService>(),
            new DataExportFactory(csvExport),
            Substitute.For<IAppPreferences>(),
            Substitute.For<ISecureStorageService>(),
            fileSystem,
            Substitute.For<IPageDialogService>(),
            Substitute.For<ILogger<DataUploadViewModel>>());
    }
}
