namespace QuickStockTaker.Core.Services.Interfaces
{
    internal interface IStocktakeRemoteTransferAdapter
    {
        StocktakeRemoteProtocol Protocol { get; }

        Task TransferAsync(
            StocktakeExport export,
            StocktakeRemoteConfiguration configuration,
            CancellationToken cancellationToken = default);
    }
}
