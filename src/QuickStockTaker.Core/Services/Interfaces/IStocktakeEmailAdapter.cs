namespace QuickStockTaker.Core.Services.Interfaces
{
    internal interface IStocktakeEmailAdapter
    {
        Task SendAsync(
            StocktakeEmailDelivery delivery,
            CancellationToken cancellationToken = default);
    }
}
