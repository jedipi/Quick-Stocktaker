using FluentAssertions;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeOperationsIsolationTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"quick-stocktaker-operations-{Guid.NewGuid():N}.db3");
    private readonly SQLiteDB _database;
    private readonly IStocktakeWorkspace _workspace;
    private readonly IStocktakeOperationsService _operations;

    public StocktakeOperationsIsolationTests()
    {
        SQLitePCL.Batteries_V2.Init();
        _database = new SQLiteDB(_databasePath);
        var preferences = new TestAppPreferences();
        _workspace = new StocktakeWorkspace(
            new SQLiteRepository<Stocktake>(_database),
            _database,
            preferences);
        _operations = new StocktakeOperationsService(
            new SQLiteRepository<StocktakeItem>(_database),
            _database,
            _workspace,
            preferences);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _database.Database.CloseAsync();
        File.Delete(_databasePath);
    }

    [Fact]
    public async Task BayAndMetadataOperations_ModifyOnlyCurrentStocktake()
    {
        var first = await _workspace.CreateAsync(
            new StocktakeDefinition("FIRST", "Sydney", new DateTime(2026, 8, 30)),
            cancellationToken: TestContext.Current.CancellationToken);
        await _operations.AddItemAsync(
            new StocktakeItem { BayLocation = "SHARED", Barcode = "FIRST-ITEM", Qty = 2 },
            TestContext.Current.CancellationToken);
        var second = await _workspace.CreateAsync(
            new StocktakeDefinition("SECOND", "Melbourne", new DateTime(2026, 8, 31)),
            cancellationToken: TestContext.Current.CancellationToken);
        await _operations.AddItemAsync(
            new StocktakeItem { BayLocation = "SHARED", Barcode = "SECOND-ITEM", Qty = 5 },
            TestContext.Current.CancellationToken);

        await _operations.DeleteBayAsync("SHARED");
        await _workspace.SelectAsync(first.Id, TestContext.Current.CancellationToken);
        await _operations.RenameBayAsync("SHARED", "RENAMED-BAY");
        await _operations.UpdateStocktakeNumberAsync("RENAMED");

        (await _operations.GetItemsAsync(TestContext.Current.CancellationToken))
            .Should().ContainSingle(item =>
                item.Barcode == "FIRST-ITEM" && item.BayLocation == "RENAMED-BAY");
        await _workspace.SelectAsync(second.Id, TestContext.Current.CancellationToken);
        (await _operations.GetItemsAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
        var saved = await _workspace.GetSavedAsync(TestContext.Current.CancellationToken);
        saved.Single(stocktake => stocktake.Id == first.Id).StocktakeNumber.Should().Be("RENAMED");
        saved.Single(stocktake => stocktake.Id == second.Id).StocktakeNumber.Should().Be("SECOND");
    }

    [Fact]
    public async Task CurrentItemOperations_KeepSavedStocktakesIsolated()
    {
        var first = await _workspace.CreateAsync(
            new StocktakeDefinition("FIRST", "Sydney", new DateTime(2026, 8, 30)),
            cancellationToken: TestContext.Current.CancellationToken);
        await _operations.AddItemAsync(
            new StocktakeItem { BayLocation = "A1", Barcode = "FIRST-ITEM", Qty = 2 },
            TestContext.Current.CancellationToken);
        var second = await _workspace.CreateAsync(
            new StocktakeDefinition("SECOND", "Melbourne", new DateTime(2026, 8, 31)),
            cancellationToken: TestContext.Current.CancellationToken);
        await _operations.AddItemAsync(
            new StocktakeItem { BayLocation = "A1", Barcode = "SECOND-ITEM", Qty = 5 },
            TestContext.Current.CancellationToken);

        var secondItems = await _operations.GetItemsAsync(TestContext.Current.CancellationToken);
        await _workspace.SelectAsync(first.Id, TestContext.Current.CancellationToken);
        var firstItems = await _operations.GetItemsAsync(TestContext.Current.CancellationToken);

        secondItems.Should().ContainSingle(item =>
            item.StocktakeId == second.Id && item.Barcode == "SECOND-ITEM");
        firstItems.Should().ContainSingle(item =>
            item.StocktakeId == first.Id && item.Barcode == "FIRST-ITEM");
    }
}
