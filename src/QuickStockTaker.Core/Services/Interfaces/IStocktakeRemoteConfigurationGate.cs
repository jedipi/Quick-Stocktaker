namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IStocktakeRemoteConfigurationGate
    {
        Task RunAsync(
            Func<Task> action,
            CancellationToken cancellationToken = default);

        Task<T> RunAsync<T>(
            Func<Task<T>> action,
            CancellationToken cancellationToken = default);
    }
}
