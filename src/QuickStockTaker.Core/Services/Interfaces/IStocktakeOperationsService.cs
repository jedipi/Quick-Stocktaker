using QuickStockTaker.Core.Models.Sqlite;

namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IStocktakeOperationsService
    {
        Task<IReadOnlyList<StocktakeItem>> GetItemsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StocktakeItem>> GetItemsAsync(
            int stocktakeId,
            CancellationToken cancellationToken = default);

        Task<int> AddItemAsync(
            StocktakeItem item,
            CancellationToken cancellationToken = default);

        Task<int> DeleteItemAsync(
            StocktakeItem item,
            CancellationToken cancellationToken = default);

        Task<int> DeleteBayAsync(string bayLocation);

        Task<int> RenameBayAsync(string currentBayLocation, string newBayLocation);

        Task<int> UpdateItemAsync(StocktakeItem item);

        Task<int> UpdateStocktakeNumberAsync(string stocktakeNumber);

        Task<int> UpdateSiteAsync(string site);

        Task<int> UpdateStocktakeDateAsync(string stocktakeDate);
    }
}
