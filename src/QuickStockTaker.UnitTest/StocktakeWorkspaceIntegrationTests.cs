using FluentAssertions;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using SQLite;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeWorkspaceIntegrationTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"quick-stocktaker-{Guid.NewGuid():N}.db3");
    private readonly SQLiteDB _database;

    public StocktakeWorkspaceIntegrationTests()
    {
        SQLitePCL.Batteries_V2.Init();
        _database = new SQLiteDB(_databasePath);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _database.Database.CloseAsync();
        File.Delete(_databasePath);
    }

    [Fact]
    public async Task InitializeAsync_WhenLegacyStocktakeIsEmpty_PreservesConfiguredStocktakeOnce()
    {
        var preferences = new TestAppPreferences();
        preferences.Set(QuickStockTaker.Core.Data.Constants.StocktakeNumber, "EMPTY");
        preferences.Set(QuickStockTaker.Core.Data.Constants.Site, "Darwin");
        preferences.Set(
            QuickStockTaker.Core.Data.Constants.StocktakeDate,
            new DateTime(2026, 9, 1));
        var workspace = CreateWorkspace(preferences);

        await workspace.InitializeAsync(TestContext.Current.CancellationToken);
        var restartedWorkspace = CreateWorkspace(preferences);
        await restartedWorkspace.InitializeAsync(TestContext.Current.CancellationToken);

        var saved = await restartedWorkspace.GetSavedAsync(TestContext.Current.CancellationToken);
        saved.Should().ContainSingle(stocktake =>
            stocktake.StocktakeNumber == "EMPTY" &&
            stocktake.Site == "Darwin" &&
            stocktake.ItemCount == 0 &&
            stocktake.IsCurrent);
    }

    [Fact]
    public async Task CreateAndSelectAsync_AllowsDuplicateNumbersAndChangesCurrentStocktake()
    {
        var workspace = CreateWorkspace();
        var first = await workspace.CreateAsync(
            new StocktakeDefinition("COUNT-1", "Sydney", new DateTime(2026, 8, 30)),
            cancellationToken: TestContext.Current.CancellationToken);
        var second = await workspace.CreateAsync(
            new StocktakeDefinition("COUNT-1", "Melbourne", new DateTime(2026, 8, 31)),
            cancellationToken: TestContext.Current.CancellationToken);
        await _database.Database.ExecuteAsync(
            "UPDATE Stocktake SET UpdatedAt = ?",
            DateTime.UtcNow);

        var savedStocktakes = await workspace.GetSavedAsync(TestContext.Current.CancellationToken);
        savedStocktakes.Select(stocktake => stocktake.Id).Should().Equal(second.Id, first.Id);
        savedStocktakes.Single(stocktake => stocktake.Id == second.Id).IsCurrent.Should().BeTrue();

        var selected = await workspace.SelectAsync(
            first.Id,
            TestContext.Current.CancellationToken);

        selected.Should().BeTrue();
        var current = await workspace.GetCurrentAsync(TestContext.Current.CancellationToken);
        current!.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task GetSavedAsync_AggregatesItemsAndUnitsForEachStocktake()
    {
        var workspace = CreateWorkspace();
        var first = await workspace.CreateAsync(
            new StocktakeDefinition("FIRST", "Sydney", new DateTime(2026, 8, 30)),
            cancellationToken: TestContext.Current.CancellationToken);
        var second = await workspace.CreateAsync(
            new StocktakeDefinition("SECOND", "Melbourne", new DateTime(2026, 8, 31)),
            cancellationToken: TestContext.Current.CancellationToken);
        await _database.Database.InsertAllAsync(new[]
        {
            CreateItem(first.Id, 2),
            CreateItem(first.Id, 3),
            CreateItem(second.Id, 7)
        });

        var saved = await workspace.GetSavedAsync(TestContext.Current.CancellationToken);

        saved.Single(stocktake => stocktake.Id == first.Id).Should().Match<SavedStocktakeSummary>(
            stocktake => stocktake.ItemCount == 2 && stocktake.UnitCount == 5);
        saved.Single(stocktake => stocktake.Id == second.Id).Should().Match<SavedStocktakeSummary>(
            stocktake => stocktake.ItemCount == 1 && stocktake.UnitCount == 7);

        static StocktakeItem CreateItem(int stocktakeId, long quantity) => new()
        {
            StocktakeId = stocktakeId,
            DeviceId = "SCANNER",
            StocktakeNumber = "COUNT",
            Site = "SITE",
            BayLocation = "A1",
            Barcode = Guid.NewGuid().ToString("N"),
            Description = "Item",
            Qty = quantity,
            StocktakeDate = "2026-08-30",
            InsertedAt = "2026-08-30",
            UpdatedAt = "2026-08-30"
        };
    }

    [Fact]
    public async Task DeleteAndReplaceAsync_SelectsNewestRemainingAndRemovesOnlyReplacementTarget()
    {
        var workspace = CreateWorkspace();
        var first = await workspace.CreateAsync(
            new StocktakeDefinition("FIRST", "Sydney", new DateTime(2026, 8, 30)),
            cancellationToken: TestContext.Current.CancellationToken);
        var second = await workspace.CreateAsync(
            new StocktakeDefinition("SECOND", "Melbourne", new DateTime(2026, 8, 31)),
            cancellationToken: TestContext.Current.CancellationToken);
        await workspace.SelectAsync(first.Id, TestContext.Current.CancellationToken);

        var deleted = await workspace.DeleteAsync(first.Id, TestContext.Current.CancellationToken);

        deleted.Should().BeTrue();
        (await workspace.GetCurrentAsync(TestContext.Current.CancellationToken))!.Id
            .Should().Be(second.Id);

        var replacement = await workspace.CreateAsync(
            new StocktakeDefinition("REPLACEMENT", "Perth", new DateTime(2026, 9, 1)),
            replaceStocktakeId: second.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        var saved = await workspace.GetSavedAsync(TestContext.Current.CancellationToken);
        saved.Should().ContainSingle(summary => summary.Id == replacement.Id && summary.IsCurrent);
    }

    [Fact]
    public async Task InitializeAsync_WhenLegacyStocktakeExists_MigratesItAsCurrentWithoutDataLoss()
    {
        await _database.Database.CreateTableAsync<LegacyStocktakeItem>();
        await _database.Database.InsertAsync(new LegacyStocktakeItem
        {
            DeviceId = "SCANNER-1",
            StocktakeNumber = "COUNT-42",
            Site = "Sydney",
            BayLocation = "A1",
            Barcode = "123456789",
            Description = "Widget",
            Qty = 7,
            StocktakeDate = "2026-08-30",
            InsertedAt = "30/08/2026 9:00 AM",
            UpdatedAt = "30/08/2026 9:00 AM"
        });
        var preferences = new TestAppPreferences();
        var workspace = CreateWorkspace(preferences);

        await workspace.InitializeAsync(TestContext.Current.CancellationToken);

        var savedStocktakes = await workspace.GetSavedAsync(TestContext.Current.CancellationToken);
        var current = await workspace.GetCurrentAsync(TestContext.Current.CancellationToken);
        savedStocktakes.Should().ContainSingle();
        savedStocktakes[0].StocktakeNumber.Should().Be("COUNT-42");
        savedStocktakes[0].Site.Should().Be("Sydney");
        savedStocktakes[0].StocktakeDate.Should().Be(new DateTime(2026, 8, 30));
        savedStocktakes[0].ItemCount.Should().Be(1);
        savedStocktakes[0].UnitCount.Should().Be(7);
        savedStocktakes[0].IsCurrent.Should().BeTrue();
        current.Should().NotBeNull();
        current!.Id.Should().Be(savedStocktakes[0].Id);
        preferences.GetString(QuickStockTaker.Core.Data.Constants.StocktakeNumber, "")
            .Should().Be("COUNT-42");
        preferences.GetString(QuickStockTaker.Core.Data.Constants.Site, "")
            .Should().Be("Sydney");
        preferences.GetDateTime(QuickStockTaker.Core.Data.Constants.StocktakeDate, DateTime.MinValue)
            .Should().Be(new DateTime(2026, 8, 30));

        var restartedWorkspace = CreateWorkspace(preferences);
        await restartedWorkspace.InitializeAsync(TestContext.Current.CancellationToken);
        (await restartedWorkspace.GetSavedAsync(TestContext.Current.CancellationToken))
            .Should().ContainSingle(stocktake => stocktake.Id == current.Id);
    }

    [Fact]
    public async Task InitializeAsync_WhenPreferencesDiffer_SynchronizesLegacyItemMetadata()
    {
        await _database.Database.CreateTableAsync<LegacyStocktakeItem>();
        await _database.Database.InsertAsync(new LegacyStocktakeItem
        {
            DeviceId = "SCANNER-1",
            StocktakeNumber = "ITEM-COUNT",
            Site = "Item Site",
            BayLocation = "A1",
            Barcode = "123456789",
            Description = "Widget",
            Qty = 7,
            StocktakeDate = "2026-08-30",
            InsertedAt = "30/08/2026 9:00 AM",
            UpdatedAt = "30/08/2026 9:00 AM"
        });
        var preferences = new TestAppPreferences();
        var authoritativeDate = new DateTime(2026, 9, 2);
        preferences.Set(QuickStockTaker.Core.Data.Constants.StocktakeNumber, "PARENT-COUNT");
        preferences.Set(QuickStockTaker.Core.Data.Constants.Site, "Parent Site");
        preferences.Set(QuickStockTaker.Core.Data.Constants.StocktakeDate, authoritativeDate);
        var workspace = CreateWorkspace(preferences);

        await workspace.InitializeAsync(TestContext.Current.CancellationToken);

        var migratedItem = await _database.Database.Table<StocktakeItem>().FirstAsync();
        migratedItem.StocktakeId.Should().BeGreaterThan(0);
        migratedItem.StocktakeNumber.Should().Be("PARENT-COUNT");
        migratedItem.Site.Should().Be("Parent Site");
        migratedItem.StocktakeDate.Should().Be(authoritativeDate.ToShortDateString());
    }

    private IStocktakeWorkspace CreateWorkspace(TestAppPreferences? preferences = null) =>
        new StocktakeWorkspace(
            new SQLiteRepository<Stocktake>(_database),
            _database,
            preferences ?? new TestAppPreferences());

    [Table("StocktakeItem")]
    private sealed class LegacyStocktakeItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string DeviceId { get; set; } = "";
        public string StocktakeNumber { get; set; } = "";
        public string Site { get; set; } = "";
        public string BayLocation { get; set; } = "";
        public string Barcode { get; set; } = "";
        public string Description { get; set; } = "";
        public long Qty { get; set; }
        public string StocktakeDate { get; set; } = "";
        public string InsertedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";
    }
}
