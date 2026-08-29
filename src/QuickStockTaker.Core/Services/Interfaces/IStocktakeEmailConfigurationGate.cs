namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IStocktakeEmailConfigurationGate
    {
        Task RunAsync(Func<Task> action);

        Task<T> RunAsync<T>(Func<Task<T>> action);
    }
}
