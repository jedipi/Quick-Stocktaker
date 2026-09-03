using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services;

public sealed class StocktakeWorkspace : IStocktakeWorkspace
{
    private readonly ISQLiteRepository<Stocktake> _stocktakeRepository;
    private readonly SQLiteDB _database;
    private readonly IAppPreferences _preferences;
    private readonly object _initializationLock = new();

    private Task _initializationTask;

    public StocktakeWorkspace(
        ISQLiteRepository<Stocktake> stocktakeRepository,
        SQLiteDB database,
        IAppPreferences preferences)
    {
        _stocktakeRepository = stocktakeRepository;
        _database = database;
        _preferences = preferences;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Task initializationTask;
        lock (_initializationLock)
        {
            _initializationTask ??= InitializeCoreAsync();
            initializationTask = _initializationTask;
        }

        try
        {
            await initializationTask.WaitAsync(cancellationToken);
        }
        catch when (initializationTask.IsFaulted)
        {
            lock (_initializationLock)
            {
                if (ReferenceEquals(_initializationTask, initializationTask))
                    _initializationTask = null;
            }

            throw;
        }
    }

    public async Task<Stocktake> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var currentStocktakeId = _preferences.GetInt(Constants.CurrentStocktakeId, 0);
        if (currentStocktakeId == 0)
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        var current = await _stocktakeRepository.GetByIdAsync(currentStocktakeId);
        cancellationToken.ThrowIfCancellationRequested();
        return current;
    }

    public async Task<IReadOnlyList<SavedStocktakeSummary>> GetSavedAsync(
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var stocktakes = (await _stocktakeRepository.GetAllAsync())
            .OrderByDescending(stocktake => stocktake.UpdatedAt)
            .ThenByDescending(stocktake => stocktake.Id)
            .ToList();
        cancellationToken.ThrowIfCancellationRequested();
        var itemAggregates = await _database.Database.QueryAsync<StocktakeItemAggregate>(
            "SELECT StocktakeId, COUNT(*) AS ItemCount, COALESCE(SUM(Qty), 0) AS UnitCount " +
            "FROM StocktakeItem GROUP BY StocktakeId");
        cancellationToken.ThrowIfCancellationRequested();
        var itemAggregatesByStocktakeId = itemAggregates.ToDictionary(
            aggregate => aggregate.StocktakeId);
        var currentStocktakeId = _preferences.GetInt(Constants.CurrentStocktakeId, 0);

        return stocktakes.Select(stocktake =>
        {
            itemAggregatesByStocktakeId.TryGetValue(stocktake.Id, out var aggregate);
            return new SavedStocktakeSummary(
                stocktake.Id,
                stocktake.StocktakeNumber,
                stocktake.Site,
                stocktake.StocktakeDate,
                stocktake.UpdatedAt,
                aggregate?.ItemCount ?? 0,
                aggregate?.UnitCount ?? 0,
                stocktake.Id == currentStocktakeId);
        }).ToList();
    }

    public async Task<Stocktake> CreateAsync(
        StocktakeDefinition definition,
        int? replaceStocktakeId = null,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var stocktake = new Stocktake
        {
            StocktakeNumber = definition.StocktakeNumber,
            Site = definition.Site,
            StocktakeDate = definition.StocktakeDate,
            UpdatedAt = now
        };

        await _database.Database.RunInTransactionAsync(connection =>
        {
            connection.Insert(stocktake);
            if (replaceStocktakeId.HasValue)
            {
                connection.Execute(
                    "DELETE FROM StocktakeItem WHERE StocktakeId = ?",
                    replaceStocktakeId.Value);
                connection.Execute(
                    "DELETE FROM Stocktake WHERE Id = ?",
                    replaceStocktakeId.Value);
            }
        });

        SetCurrentStocktake(stocktake);
        return stocktake;
    }

    public async Task<bool> SelectAsync(
        int stocktakeId,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var stocktake = await _stocktakeRepository.GetByIdAsync(stocktakeId);
        cancellationToken.ThrowIfCancellationRequested();
        if (stocktake is null)
            return false;

        SetCurrentStocktake(stocktake);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int stocktakeId,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var stocktake = await _stocktakeRepository.GetByIdAsync(stocktakeId);
        cancellationToken.ThrowIfCancellationRequested();
        if (stocktake is null)
            return false;

        await _database.Database.RunInTransactionAsync(connection =>
        {
            connection.Execute(
                "DELETE FROM StocktakeItem WHERE StocktakeId = ?",
                stocktakeId);
            connection.Execute("DELETE FROM Stocktake WHERE Id = ?", stocktakeId);
        });

        var currentStocktakeId = _preferences.GetInt(Constants.CurrentStocktakeId, 0);
        if (currentStocktakeId != stocktakeId)
            return true;

        var newestRemaining = await _database.Database.Table<Stocktake>()
            .OrderByDescending(candidate => candidate.UpdatedAt)
            .ThenByDescending(candidate => candidate.Id)
            .FirstOrDefaultAsync();
        if (newestRemaining is null)
            ClearCurrentStocktake();
        else
            SetCurrentStocktake(newestRemaining);

        return true;
    }

    private async Task InitializeCoreAsync()
    {
        await _database.Database.CreateTableAsync<Stocktake>();
        await _database.Database.CreateTableAsync<StocktakeItem>();

        var stocktakes = await _stocktakeRepository.GetAllAsync();
        if (stocktakes.Count == 0)
        {
            await MigrateLegacyStocktakeAsync();
            stocktakes = await _stocktakeRepository.GetAllAsync();
        }

        if (stocktakes.Count == 0)
        {
            _preferences.Set(Constants.CurrentStocktakeId, 0);
            return;
        }

        var currentStocktakeId = _preferences.GetInt(Constants.CurrentStocktakeId, 0);
        var currentExists = stocktakes.Any(stocktake => stocktake.Id == currentStocktakeId);
        if (!currentExists)
        {
            var newestStocktake = stocktakes
                .OrderByDescending(stocktake => stocktake.UpdatedAt)
                .ThenByDescending(stocktake => stocktake.Id)
                .First();
            SetCurrentStocktake(newestStocktake);
            currentStocktakeId = newestStocktake.Id;
        }

        await _database.Database.ExecuteAsync(
            "UPDATE StocktakeItem SET StocktakeId = ? WHERE StocktakeId IS NULL OR StocktakeId = 0",
            currentStocktakeId);
    }

    private async Task MigrateLegacyStocktakeAsync()
    {
        var legacyItems = await _database.Database.Table<StocktakeItem>().ToListAsync();
        var stocktakeNumber = _preferences.GetString(Constants.StocktakeNumber, "");
        var site = _preferences.GetString(Constants.Site, "");
        var stocktakeDate = _preferences.GetDateTime(Constants.StocktakeDate, DateTime.MinValue);
        var firstItem = legacyItems.FirstOrDefault();

        stocktakeNumber = string.IsNullOrWhiteSpace(stocktakeNumber)
            ? firstItem?.StocktakeNumber ?? ""
            : stocktakeNumber;
        site = string.IsNullOrWhiteSpace(site) ? firstItem?.Site ?? "" : site;
        if (stocktakeDate == DateTime.MinValue &&
            !DateTime.TryParse(firstItem?.StocktakeDate, out stocktakeDate))
        {
            stocktakeDate = DateTime.Today;
        }

        if (legacyItems.Count == 0 &&
            string.IsNullOrWhiteSpace(stocktakeNumber) &&
            string.IsNullOrWhiteSpace(site))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var stocktake = new Stocktake
        {
            StocktakeNumber = stocktakeNumber,
            Site = site,
            StocktakeDate = stocktakeDate,
            UpdatedAt = now
        };

        await _database.Database.RunInTransactionAsync(connection =>
        {
            connection.Insert(stocktake);
            connection.Execute(
                "UPDATE StocktakeItem " +
                "SET StocktakeId = ?, StocktakeNumber = ?, Site = ?, StocktakeDate = ? " +
                "WHERE StocktakeId IS NULL OR StocktakeId = 0",
                stocktake.Id,
                stocktake.StocktakeNumber,
                stocktake.Site,
                stocktake.StocktakeDate.ToShortDateString());
        });
        SetCurrentStocktake(stocktake);
    }

    private void SetCurrentStocktake(Stocktake stocktake)
    {
        _preferences.Set(Constants.CurrentStocktakeId, stocktake.Id);
        _preferences.Set(Constants.StocktakeNumber, stocktake.StocktakeNumber);
        _preferences.Set(Constants.Site, stocktake.Site);
        _preferences.Set(Constants.StocktakeDate, stocktake.StocktakeDate);
    }

    private void ClearCurrentStocktake()
    {
        _preferences.Set(Constants.CurrentStocktakeId, 0);
        _preferences.Set(Constants.StocktakeNumber, "");
        _preferences.Set(Constants.Site, "");
        _preferences.Set(Constants.StocktakeDate, DateTime.MinValue);
    }

    private sealed class StocktakeItemAggregate
    {
        public int StocktakeId { get; set; }

        public int ItemCount { get; set; }

        public long UnitCount { get; set; }
    }
}
