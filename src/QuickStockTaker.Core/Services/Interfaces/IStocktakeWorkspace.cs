using QuickStockTaker.Core.Models.Sqlite;

namespace QuickStockTaker.Core.Services.Interfaces;

public interface IStocktakeWorkspace
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<Stocktake> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedStocktakeSummary>> GetSavedAsync(
        CancellationToken cancellationToken = default);

    Task<Stocktake> CreateAsync(
        StocktakeDefinition definition,
        int? replaceStocktakeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> SelectAsync(int stocktakeId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int stocktakeId, CancellationToken cancellationToken = default);
}

public sealed record StocktakeDefinition(
    string StocktakeNumber,
    string Site,
    DateTime StocktakeDate);

public sealed record SavedStocktakeSummary(
    int Id,
    string StocktakeNumber,
    string Site,
    DateTime StocktakeDate,
    DateTime UpdatedAt,
    int ItemCount,
    long UnitCount,
    bool IsCurrent);
