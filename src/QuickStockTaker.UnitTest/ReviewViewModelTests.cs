using CommunityToolkit.Maui;
using Controls.UserDialogs.Maui;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public class ReviewViewModelTests
{
    [Fact]
    public async Task Appearing_LoadsBayAndQuantityTotalsFromStocktakeItems()
    {
        IReadOnlyList<StocktakeItem> items =
        [
            new StocktakeItem { BayLocation = "A1", Qty = 2 },
            new StocktakeItem { BayLocation = "A1", Qty = 3 },
            new StocktakeItem { BayLocation = "B2", Qty = 5 }
        ];
        var stocktakeOperations = Substitute.For<IStocktakeOperationsService>();
        stocktakeOperations.GetItemsAsync(Arg.Any<CancellationToken>()).Returns(items);

        var viewModel = new ReviewViewModel(
            Substitute.For<IUserDialogs>(),
            Substitute.For<IPopupService>(),
            Substitute.For<ILogger<ReviewViewModel>>(),
            new TestAppPreferences(),
            stocktakeOperations,
            Substitute.For<IPageDialogService>());

        await viewModel.AppearingCommand.ExecuteAsync(null);

        viewModel.BaysCounts.Should().Be(2);
        viewModel.ItemCounts.Should().Be(10);
    }
}
