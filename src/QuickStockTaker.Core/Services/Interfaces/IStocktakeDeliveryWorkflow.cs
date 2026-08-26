using QuickStockTaker.Core.Services;

namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IStocktakeDeliveryWorkflow
    {
        Task<StocktakeDeliveryResult> CreateExportAsync(CancellationToken cancellationToken = default);
    }
}
