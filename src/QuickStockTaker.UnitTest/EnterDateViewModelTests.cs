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
        var viewModel = CreateViewModel(dialogs, repository, out _, out _);

        await viewModel.AddItemCommand.ExecuteAsync(null);

        var items = await repository.GetAllAsync();
        items.Should().HaveCount(250);
        items[^1].Barcode.Should().Be("NEW-BARCODE");
    }

    [Fact]
    public async Task AddItemCommand_WhenContinuousFreeStocktakeReaches250_ClosesAndOffersUpgrade()
    {
        var repository = CreateRepositoryWithItemCount(249);
        var dialogs = Substitute.For<IUserDialogs>();
        var viewModel = CreateViewModel(
            dialogs,
            repository,
            out var cameraPopupService,
            out _,
            continuousMode: true);

        await viewModel.AddItemCommand.ExecuteAsync(null);

        (await repository.GetAllAsync()).Should().HaveCount(250);
        await cameraPopupService.Received(1).CloseAsync();
        await dialogs.Received(1).ConfirmAsync(
            "The free version supports up to 250 saved items per stocktake. Upgrade to Pro for unlimited items.",
            "Free item limit reached",
            "Upgrade",
            "Not now",
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemCommand_WhenFreeStocktakeHas250Items_OffersUpgradeAndDoesNotInsert()
    {
        var repository = CreateRepositoryWithItemCount(250);
        var dialogs = Substitute.For<IUserDialogs>();
        dialogs.ConfirmAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var viewModel = CreateViewModel(
            dialogs,
            repository,
            out var cameraPopupService,
            out var navigationService);

        await viewModel.AddItemCommand.ExecuteAsync(null);

        var items = await repository.GetAllAsync();
        items.Should().HaveCount(250);
        viewModel.Barcode.Should().Be("NEW-BARCODE");
        await cameraPopupService.Received(1).CloseAsync();
        await dialogs.Received(1).ConfirmAsync(
            "The free version supports up to 250 saved items per stocktake. Upgrade to Pro for unlimited items.",
            "Free item limit reached",
            "Upgrade",
            "Not now",
            null,
            Arg.Any<CancellationToken>());
        await navigationService.Received(1).GoToAsync("//AboutPage");
    }

    [Fact]
    public async Task AddItemCommand_WhenProStocktakeHas250Items_InsertsAnotherItem()
    {
        var repository = CreateRepositoryWithItemCount(250);
        var dialogs = Substitute.For<IUserDialogs>();
        var viewModel = CreateViewModel(dialogs, repository, out _, out _, isPro: true);

        await viewModel.AddItemCommand.ExecuteAsync(null);

        var items = await repository.GetAllAsync();
        items.Should().HaveCount(251);
        items[^1].Barcode.Should().Be("NEW-BARCODE");
    }

    private static EnterDateViewModel CreateViewModel(
        IUserDialogs dialogs,
        TestStocktakeItemRepository repository,
        out ICameraPopupService cameraPopupService,
        out INavigationService navigationService,
        bool isPro = false,
        bool continuousMode = false)
    {
        cameraPopupService = Substitute.For<ICameraPopupService>();
        navigationService = Substitute.For<INavigationService>();
        var entitlementService = Substitute.For<IEntitlementService>();
        entitlementService.IsPro.Returns(isPro);
        var stocktakeOperations = Substitute.For<IStocktakeOperationsService>();
        stocktakeOperations.GetItemsAsync(Arg.Any<CancellationToken>())
            .Returns(async _ => (IReadOnlyList<StocktakeItem>)await repository.GetAllAsync());
        stocktakeOperations.AddItemAsync(
                Arg.Any<StocktakeItem>(),
                Arg.Any<CancellationToken>())
            .Returns(call => repository.InsertAsync(call.Arg<StocktakeItem>()));

        var preferences = new TestAppPreferences();
        preferences.Set(QuickStockTaker.Core.Data.Constants.ContinuousMode, continuousMode);

        return new EnterDateViewModel(
            dialogs,
            cameraPopupService,
            Substitute.For<ILogger<EnterDateViewModel>>(),
            stocktakeOperations,
            preferences,
            entitlementService,
            navigationService)
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
