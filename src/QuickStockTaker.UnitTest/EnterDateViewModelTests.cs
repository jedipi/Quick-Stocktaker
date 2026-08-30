using Controls.UserDialogs.Maui;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public sealed class EnterDateViewModelTests
{
    [Fact]
    public async Task AddItemCommand_WhenStocktakeHas249Items_Inserts250thItem()
    {
        var repository = CreateRepositoryWithItemCount(249);
        var dialogs = Substitute.For<IUserDialogs>();
        var viewModel = CreateViewModel(dialogs, repository);

        await viewModel.AddItemCommand.ExecuteAsync(null);

        var items = await repository.GetAllAsync();
        items.Should().HaveCount(250);
        items[^1].Barcode.Should().Be("NEW-BARCODE");
    }

    [Fact]
    public async Task AddItemCommand_WhenStocktakeHas250Items_ShowsLimitAndDoesNotInsert()
    {
        var repository = CreateRepositoryWithItemCount(250);
        var dialogs = Substitute.For<IUserDialogs>();
        var viewModel = CreateViewModel(dialogs, repository);

        await viewModel.AddItemCommand.ExecuteAsync(null);

        var items = await repository.GetAllAsync();
        items.Should().HaveCount(250);
        viewModel.Barcode.Should().Be("NEW-BARCODE");
        await dialogs.Received(1).AlertAsync(
            "You have reached the 250 item scan limit.",
            "Scan limit reached",
            "OK",
            null,
            Arg.Any<CancellationToken>());
    }

    private static EnterDateViewModel CreateViewModel(
        IUserDialogs dialogs,
        TestStocktakeItemRepository repository)
    {
        return new EnterDateViewModel(
            dialogs,
            Substitute.For<ICameraPopupService>(),
            Substitute.For<ILogger<EnterDateViewModel>>(),
            repository,
            new TestAppPreferences())
        {
            BayLocation = "A1",
            Barcode = "NEW-BARCODE"
        };
    }

    private static TestStocktakeItemRepository CreateRepositoryWithItemCount(int itemCount)
    {
        var items = Enumerable.Range(0, itemCount)
            .Select(index => new StocktakeItem { Barcode = $"BARCODE-{index}" });

        return new TestStocktakeItemRepository(items);
    }
}
