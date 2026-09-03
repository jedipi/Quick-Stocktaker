using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Repositories.Interfaces;

namespace QuickStockTaker.Core.Services
{
    public class StocktakeOperationsService : IStocktakeOperationsService
    {
        private readonly ISQLiteRepository<StocktakeItem> _itemRepository;
        private readonly SQLiteDB _database;
        private readonly IStocktakeWorkspace _workspace;
        private readonly IAppPreferences _preferences;

        public StocktakeOperationsService(
            ISQLiteRepository<StocktakeItem> itemRepository,
            SQLiteDB database,
            IStocktakeWorkspace workspace,
            IAppPreferences preferences)
        {
            _itemRepository = itemRepository;
            _database = database;
            _workspace = workspace;
            _preferences = preferences;
        }

        public async Task<IReadOnlyList<StocktakeItem>> GetItemsAsync(
            CancellationToken cancellationToken = default)
        {
            var current = await _workspace.GetCurrentAsync(cancellationToken);
            if (current is null)
                return [];

            return await GetItemsAsync(current.Id, cancellationToken);
        }

        public async Task<IReadOnlyList<StocktakeItem>> GetItemsAsync(
            int stocktakeId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = await _itemRepository.FindAsync(item => item.StocktakeId == stocktakeId);
            cancellationToken.ThrowIfCancellationRequested();
            return items;
        }

        public async Task<int> AddItemAsync(
            StocktakeItem item,
            CancellationToken cancellationToken = default)
        {
            var current = await _workspace.GetCurrentAsync(cancellationToken);
            if (current is null)
                return 0;

            item.StocktakeId = current.Id;
            item.DeviceId ??= _preferences.GetString(Constants.DeviceId, "");
            item.StocktakeNumber = current.StocktakeNumber;
            item.Site = current.Site;
            item.StocktakeDate = current.StocktakeDate.ToShortDateString();
            item.Description ??= "";
            var timestamp = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
            item.InsertedAt ??= timestamp;
            item.UpdatedAt = timestamp;

            var result = await _itemRepository.InsertAsync(item);
            await TouchCurrentAsync(current.Id);
            return result;
        }

        public async Task<int> DeleteItemAsync(
            StocktakeItem item,
            CancellationToken cancellationToken = default)
        {
            var current = await _workspace.GetCurrentAsync(cancellationToken);
            if (current is null)
                return 0;

            var result = await _itemRepository.ExecuteAsync(
                "DELETE FROM StocktakeItem WHERE Id = ? AND StocktakeId = ?",
                item.Id,
                current.Id);
            if (result > 0)
                await TouchCurrentAsync(current.Id);
            return result;
        }

        public async Task<int> DeleteBayAsync(string bayLocation)
        {
            var current = await _workspace.GetCurrentAsync();
            if (current is null)
                return 0;

            var result = await _itemRepository.ExecuteAsync(
                "DELETE FROM StocktakeItem WHERE StocktakeId = ? AND BayLocation = ?",
                current.Id,
                bayLocation);
            if (result > 0)
                await TouchCurrentAsync(current.Id);
            return result;
        }

        public async Task<int> RenameBayAsync(string currentBayLocation, string newBayLocation)
        {
            var current = await _workspace.GetCurrentAsync();
            if (current is null)
                return 0;

            var result = await _itemRepository.ExecuteAsync(
                "UPDATE StocktakeItem SET BayLocation = ? WHERE StocktakeId = ? AND BayLocation = ?",
                newBayLocation,
                current.Id,
                currentBayLocation);
            if (result > 0)
                await TouchCurrentAsync(current.Id);
            return result;
        }

        public async Task<int> UpdateItemAsync(StocktakeItem item)
        {
            var current = await _workspace.GetCurrentAsync();
            if (current is null)
                return 0;

            var result = await _itemRepository.ExecuteAsync(
                "UPDATE StocktakeItem SET Barcode = ?, Description = ?, Qty = ?, UpdatedAt = ? WHERE Id = ? AND StocktakeId = ?",
                item.Barcode,
                item.Description,
                item.Qty,
                DateTime.Now.ToString(),
                item.Id,
                current.Id);
            if (result > 0)
                await TouchCurrentAsync(current.Id);
            return result;
        }

        public Task<int> UpdateStocktakeNumberAsync(string stocktakeNumber)
        {
            return UpdateCurrentMetadataAsync(
                nameof(Stocktake.StocktakeNumber),
                Constants.StocktakeNumber,
                stocktakeNumber);
        }

        public Task<int> UpdateSiteAsync(string site)
        {
            return UpdateCurrentMetadataAsync(nameof(Stocktake.Site), Constants.Site, site);
        }

        public async Task<int> UpdateStocktakeDateAsync(string stocktakeDate)
        {
            var current = await _workspace.GetCurrentAsync();
            if (current is null || !DateTime.TryParse(stocktakeDate, out var parsedDate))
                return 0;

            var now = DateTime.UtcNow;
            var result = 0;
            await _database.Database.RunInTransactionAsync(connection =>
            {
                result = connection.Execute(
                    "UPDATE Stocktake SET StocktakeDate = ?, UpdatedAt = ? WHERE Id = ?",
                    parsedDate,
                    now,
                    current.Id);
                connection.Execute(
                    "UPDATE StocktakeItem SET StocktakeDate = ? WHERE StocktakeId = ?",
                    stocktakeDate,
                    current.Id);
            });
            _preferences.Set(Constants.StocktakeDate, parsedDate);
            return result;
        }

        private async Task<int> UpdateCurrentMetadataAsync(
            string columnName,
            string preferenceKey,
            string value)
        {
            var current = await _workspace.GetCurrentAsync();
            if (current is null)
                return 0;

            var now = DateTime.UtcNow;
            var result = 0;
            await _database.Database.RunInTransactionAsync(connection =>
            {
                result = connection.Execute(
                    $"UPDATE Stocktake SET {columnName} = ?, UpdatedAt = ? WHERE Id = ?",
                    value,
                    now,
                    current.Id);
                connection.Execute(
                    $"UPDATE StocktakeItem SET {columnName} = ? WHERE StocktakeId = ?",
                    value,
                    current.Id);
            });

            _preferences.Set(preferenceKey, value);

            return result;
        }

        private Task TouchCurrentAsync(int stocktakeId)
        {
            return _database.Database.ExecuteAsync(
                "UPDATE Stocktake SET UpdatedAt = ? WHERE Id = ?",
                DateTime.UtcNow,
                stocktakeId);
        }
    }
}
