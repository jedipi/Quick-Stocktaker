using Controls.UserDialogs.Maui;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public sealed class DataUploadViewModelExportIsolationTests
{
    [Fact]
    public async Task CsvCommand_WhenAnotherExportCompletes_FirstActionSheetStillSavesItsOwnExport()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-viewmodel-isolation-");
        try
        {
            var firstFile = new FileInfo(Path.Combine(tempDirectory.FullName, "first.csv"));
            var secondFile = new FileInfo(Path.Combine(tempDirectory.FullName, "second.csv"));
            await File.WriteAllTextAsync(firstFile.FullName, "first export", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(secondFile.FullName, "second export", TestContext.Current.CancellationToken);
            var savedFilePath = Path.Combine(tempDirectory.FullName, "saved.csv");
            var actionSheets = new List<ActionSheetConfig>();
            var dialogs = Substitute.For<IUserDialogs>();
            dialogs.ActionSheet(Arg.Do<ActionSheetConfig>(actionSheets.Add));
            var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
            workflow.CreateExportAsync(TestContext.Current.CancellationToken)
                .ReturnsForAnyArgs(
                    StocktakeDeliveryResult.Succeeded(new StocktakeExport(firstFile)),
                    StocktakeDeliveryResult.Succeeded(new StocktakeExport(secondFile)));
            var fileSystem = Substitute.For<IAppFileSystem>();
            fileSystem.GetDownloadFilePath(Arg.Any<string>()).Returns(savedFilePath);
            var viewModel = CreateViewModel(dialogs, workflow, fileSystem);

            await viewModel.CsvCommand.ExecuteAsync(null);
            await viewModel.CsvCommand.ExecuteAsync(null);
            actionSheets[0].Options.Single(option => option.Text == "Save").Action!();

            var savedContent = await File.ReadAllTextAsync(savedFilePath, TestContext.Current.CancellationToken);
            savedContent.Should().Be("first export");
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    private static DataUploadViewModel CreateViewModel(
        IUserDialogs dialogs,
        IStocktakeDeliveryWorkflow workflow,
        IAppFileSystem fileSystem)
    {
        var csvExport = Substitute.For<ICsvExportService>();
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
            Substitute.For<IPageDialogService>(),
            Substitute.For<ILogger<DataUploadViewModel>>());
    }
}
