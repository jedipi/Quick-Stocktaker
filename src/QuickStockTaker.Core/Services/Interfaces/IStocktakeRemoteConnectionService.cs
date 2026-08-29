namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IStocktakeRemoteConnectionService
    {
        Task<(bool Success, string Message)> TestConnectionAsync(
            CancellationToken cancellationToken = default);
    }
}
