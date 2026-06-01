using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    public class StocktakeOperationsService : IStocktakeOperationsService
    {
        private readonly ISQLiteRepository<StocktakeItem> _stocktakeItemRepo;

        public StocktakeOperationsService(ISQLiteRepository<StocktakeItem> stocktakeItemRepo)
        {
            _stocktakeItemRepo = stocktakeItemRepo;
        }

        public Task<int> DeleteBayAsync(string bayLocation)
        {
            return _stocktakeItemRepo.ExecuteAsync("DELETE FROM StocktakeItem WHERE BayLocation = ?", bayLocation);
        }

        public Task<int> RenameBayAsync(string currentBayLocation, string newBayLocation)
        {
            return _stocktakeItemRepo.ExecuteAsync(
                "UPDATE StocktakeItem SET BayLocation = ? Where BayLocation = ? ",
                newBayLocation,
                currentBayLocation);
        }

        public Task<int> UpdateItemAsync(StocktakeItem item)
        {
            return _stocktakeItemRepo.ExecuteAsync(
                "UPDATE StocktakeItem SET Barcode= ?, Qty= ? Where Id= ? ",
                item.Barcode,
                item.Qty,
                item.Id);
        }

        public Task<int> UpdateStocktakeNumberAsync(string stocktakeNumber)
        {
            return _stocktakeItemRepo.ExecuteAsync("UPDATE StocktakeItem SET StocktakeNumber=?", stocktakeNumber);
        }

        public Task<int> UpdateSiteAsync(string site)
        {
            return _stocktakeItemRepo.ExecuteAsync("UPDATE StocktakeItem SET Site=?", site);
        }

        public Task<int> UpdateStocktakeDateAsync(string stocktakeDate)
        {
            return _stocktakeItemRepo.ExecuteAsync("UPDATE StocktakeItem SET StocktakeDate=?", stocktakeDate);
        }
    }
}
