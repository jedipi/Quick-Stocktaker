using NSubstitute;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public sealed class NewStocktakeViewModelTests
{
    [Fact]
    public async Task SaveCommand_AfterFreeUserCancelsReplacement_DoesNotDeleteOrCreate()
    {
        var pageDialogs = Substitute.For<IPageDialogService>();
        pageDialogs.DisplayActionSheetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string[]>())
            .Returns("Replace current Stocktake");
        pageDialogs.DisplayConfirmationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(true);
        var workspace = Substitute.For<IStocktakeWorkspace>();
        workspace.GetSavedAsync(Arg.Any<CancellationToken>())
            .Returns([
                new SavedStocktakeSummary(
                    17, "OLD", "Sydney", new DateTime(2026, 8, 30),
                    DateTime.UtcNow, 10, 10, true)
            ]);
        var viewModel = CreateViewModel(pageDialogs, workspace, isPro: false);
        viewModel.IsToggled = true;
        await viewModel.ToggledCommand.ExecuteAsync(null);
        viewModel.StocktakeNumber = "CANCELLED";
        viewModel.Site = "Melbourne";
        viewModel.StocktakeDate = new DateTime(2026, 8, 31);

        viewModel.IsToggled = false;
        await viewModel.ToggledCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        await workspace.DidNotReceive().CreateAsync(
            Arg.Any<StocktakeDefinition>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveCommand_WhenFreeUserRetainsMultipleStocktakes_DoesNotCreateAnother()
    {
        var pageDialogs = Substitute.For<IPageDialogService>();
        var workspace = Substitute.For<IStocktakeWorkspace>();
        workspace.GetSavedAsync(Arg.Any<CancellationToken>())
            .Returns([
                new SavedStocktakeSummary(
                    18, "CURRENT", "Sydney", new DateTime(2026, 8, 31),
                    DateTime.UtcNow, 10, 10, true),
                new SavedStocktakeSummary(
                    17, "OLDER", "Melbourne", new DateTime(2026, 8, 30),
                    DateTime.UtcNow.AddDays(-1), 10, 10, false)
            ]);
        var viewModel = CreateViewModel(pageDialogs, workspace, isPro: false);
        viewModel.IsDetailsVisible = true;
        viewModel.StocktakeNumber = "BLOCKED";
        viewModel.Site = "Perth";
        viewModel.StocktakeDate = new DateTime(2026, 9, 1);

        await viewModel.SaveCommand.ExecuteAsync(null);

        await workspace.DidNotReceive().CreateAsync(
            Arg.Any<StocktakeDefinition>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveCommand_WhenFreeUserChoosesReplace_ReplacesOnlyAfterValidSave()
    {
        var pageDialogs = Substitute.For<IPageDialogService>();
        pageDialogs.DisplayActionSheetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string[]>())
            .Returns("Replace current Stocktake");
        pageDialogs.DisplayConfirmationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(true);
        var workspace = Substitute.For<IStocktakeWorkspace>();
        workspace.GetSavedAsync(Arg.Any<CancellationToken>())
            .Returns([
                new SavedStocktakeSummary(
                    17, "OLD", "Sydney", new DateTime(2026, 8, 30),
                    DateTime.UtcNow, 10, 10, true)
            ]);
        var viewModel = CreateViewModel(pageDialogs, workspace, isPro: false);

        viewModel.IsToggled = true;
        await viewModel.ToggledCommand.ExecuteAsync(null);
        await workspace.DidNotReceive().DeleteAsync(
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        viewModel.StocktakeNumber = "NEW";
        viewModel.Site = "Melbourne";
        viewModel.StocktakeDate = new DateTime(2026, 8, 31);

        await viewModel.SaveCommand.ExecuteAsync(null);

        await workspace.Received(1).CreateAsync(
            Arg.Is<StocktakeDefinition>(definition =>
                definition.StocktakeNumber == "NEW" && definition.Site == "Melbourne"),
            17,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveCommand_WhenUserIsPro_CreatesWithoutReplacingSavedStocktake()
    {
        var pageDialogs = Substitute.For<IPageDialogService>();
        var workspace = Substitute.For<IStocktakeWorkspace>();
        workspace.GetSavedAsync(Arg.Any<CancellationToken>())
            .Returns([
                new SavedStocktakeSummary(
                    17, "OLD", "Sydney", new DateTime(2026, 8, 30),
                    DateTime.UtcNow, 10, 10, true)
            ]);
        var viewModel = CreateViewModel(pageDialogs, workspace, isPro: true);

        viewModel.IsToggled = true;
        await viewModel.ToggledCommand.ExecuteAsync(null);
        viewModel.StocktakeNumber = "NEW";
        viewModel.Site = "Melbourne";
        viewModel.StocktakeDate = new DateTime(2026, 8, 31);
        await viewModel.SaveCommand.ExecuteAsync(null);

        await workspace.Received(1).CreateAsync(
            Arg.Any<StocktakeDefinition>(),
            null,
            Arg.Any<CancellationToken>());
        await pageDialogs.DidNotReceive().DisplayActionSheetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string[]>());
    }

    private static NewStocktakeViewModel CreateViewModel(
        IPageDialogService pageDialogs,
        IStocktakeWorkspace workspace,
        bool isPro)
    {
        var preferences = new TestAppPreferences();
        preferences.Set(QuickStockTaker.Core.Data.Constants.DeviceId, "SCANNER-1");
        var entitlementService = Substitute.For<IEntitlementService>();
        entitlementService.IsPro.Returns(isPro);

        return new NewStocktakeViewModel(
            new StocktakeValidator(),
            preferences,
            Substitute.For<INavigationService>(),
            pageDialogs,
            workspace,
            entitlementService);
    }
}
