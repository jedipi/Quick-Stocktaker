using System.Linq.Expressions;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using SQLite;

namespace QuickStockTaker.UnitTest;

internal sealed class TestStocktakeItemRepository : ISQLiteRepository<StocktakeItem>
{
    private readonly List<StocktakeItem> _items;

    public TestStocktakeItemRepository(IEnumerable<StocktakeItem> items)
    {
        _items = items.ToList();
    }

    public List<(string Query, object[] Arguments)> ExecutedCommands { get; } = new();

    public SQLiteAsyncConnection Connection { get; set; } = null!;

    public Task<int> InsertAsync(StocktakeItem entity)
    {
        _items.Add(entity);
        return Task.FromResult(1);
    }

    public Task<int> InsertAllAsync(List<StocktakeItem> entities)
    {
        _items.AddRange(entities);
        return Task.FromResult(entities.Count);
    }

    public Task<int> DeleteAsync(StocktakeItem entity)
    {
        return Task.FromResult(_items.Remove(entity) ? 1 : 0);
    }

    public Task<int> UpdateAsync(StocktakeItem entity) => Task.FromResult(1);

    public Task<int> DeleteAsync(Expression<Func<StocktakeItem, bool>> expression) => Task.FromResult(0);

    public Task<StocktakeItem> GetByIdAsync(int id) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id)!);

    public Task<List<StocktakeItem>> GetAllAsync() => Task.FromResult(_items.ToList());

    public Task<List<StocktakeItem>> FindAsync(Expression<Func<StocktakeItem, bool>> expression) =>
        Task.FromResult(_items.AsQueryable().Where(expression).ToList());

    public Task<int> ExecuteAsync(string query, params object[] objects)
    {
        ExecutedCommands.Add((query, objects));
        return Task.FromResult(1);
    }

    public Task DropandRecreateTable()
    {
        _items.Clear();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
