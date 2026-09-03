using FluentAssertions;
using NSubstitute;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public sealed class SavedStocktakesViewModelTests
{
    [Fact]
    public async Task AppearingAndResumeCommands_ShowFreeHistoryAndSelectStocktake()
    {
        var current = new SavedStocktakeSummary(
            2, "CURRENT", "Sydney", new DateTime(2026, 8, 31),
            DateTime.UtcNow, 5, 8, true);
        var older = new SavedStocktakeSummary(
            1, "OLDER", "Melbourne", new DateTime(2026, 8, 30),
            DateTime.UtcNow.AddDays(-1), 3, 4, false);
        var workspace = Substitute.For<IStocktakeWorkspace>();
        workspace.GetSavedAsync(Arg.Any<CancellationToken>()).Returns([current, older]);
        workspace.SelectAsync(older.Id, Arg.Any<CancellationToken>()).Returns(true);
        var entitlementService = Substitute.For<IEntitlementService>();
        entitlementService.IsPro.Returns(false);
        var navigation = Substitute.For<INavigationService>();
        var viewModel = new SavedStocktakesViewModel(
            workspace,
            entitlementService,
            navigation,
            Substitute.For<IPageDialogService>());

        await viewModel.AppearingCommand.ExecuteAsync(null);
        await viewModel.ResumeCommand.ExecuteAsync(older);

        viewModel.Stocktakes.Select(stocktake => stocktake.Id).Should().Equal(2, 1);
        viewModel.ShowUpgradePrompt.Should().BeTrue();
        await workspace.Received(1).SelectAsync(1, Arg.Any<CancellationToken>());
        await navigation.Received(1).GoToAsync("..");
    }
}
