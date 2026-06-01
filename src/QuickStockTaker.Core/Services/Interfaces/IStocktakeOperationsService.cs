using QuickStockTaker.Core.Models.Sqlite;

namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IStocktakeOperationsService
    {
        Task<int> DeleteBayAsync(string bayLocation);

        Task<int> RenameBayAsync(string currentBayLocation, string newBayLocation);

        Task<int> UpdateItemAsync(StocktakeItem item);

        Task<int> UpdateStocktakeNumberAsync(string stocktakeNumber);

        Task<int> UpdateSiteAsync(string site);

        Task<int> UpdateStocktakeDateAsync(string stocktakeDate);
    }
}
