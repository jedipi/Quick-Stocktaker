using QuickStockTaker.Core.Services;

namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IStocktakeDeliveryWorkflow
    {
        Task<StocktakeDeliveryResult> CreateExportAsync(CancellationToken cancellationToken = default);

        Task<StocktakeDeliveryResult> DeliverByEmailAsync(
            string recipient,
            CancellationToken cancellationToken = default);

        Task<StocktakeDeliveryResult> DeliverToConfiguredRemoteAsync(
            CancellationToken cancellationToken = default,
            Action onTransferStarting = null);
    }
}
