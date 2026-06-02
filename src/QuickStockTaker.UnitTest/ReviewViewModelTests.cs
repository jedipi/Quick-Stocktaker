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
        var repository = new TestStocktakeItemRepository(
        [
            new StocktakeItem { BayLocation = "A1", Qty = 2 },
            new StocktakeItem { BayLocation = "A1", Qty = 3 },
            new StocktakeItem { BayLocation = "B2", Qty = 5 }
        ]);

        var viewModel = new ReviewViewModel(
            Substitute.For<IUserDialogs>(),
            Substitute.For<IPopupService>(),
            Substitute.For<ILogger<ReviewViewModel>>(),
            new TestAppPreferences(),
            new StocktakeOperationsService(repository),
            Substitute.For<IPageDialogService>(),
            repository);

        await viewModel.AppearingCommand.ExecuteAsync(null);

        viewModel.BaysCounts.Should().Be(2);
        viewModel.ItemCounts.Should().Be(10);
    }
}
