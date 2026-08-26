using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    public sealed class StocktakeDeliveryWorkflow : IStocktakeDeliveryWorkflow
    {
        private readonly ICsvExportService _csvExport;
        private readonly ILogger<StocktakeDeliveryWorkflow> _logger;
        private readonly StocktakeDeliveryOperationGate _operationGate;

        internal StocktakeDeliveryWorkflow(
            ICsvExportService csvExport,
            ILogger<StocktakeDeliveryWorkflow> logger)
            : this(csvExport, logger, new StocktakeDeliveryOperationGate())
        {
        }

        public StocktakeDeliveryWorkflow(
            ICsvExportService csvExport,
            ILogger<StocktakeDeliveryWorkflow> logger,
            StocktakeDeliveryOperationGate operationGate)
        {
            _csvExport = csvExport;
            _logger = logger;
            _operationGate = operationGate;
        }

        public async Task<StocktakeDeliveryResult> CreateExportAsync(CancellationToken cancellationToken = default)
        {
            if (!_operationGate.TryEnter())
                return StocktakeDeliveryResult.AlreadyInProgress();

            try
            {
                var export = await _csvExport.CreateExportAsync(cancellationToken);
                return export is null
                    ? StocktakeDeliveryResult.NoStocktakeData()
                    : StocktakeDeliveryResult.Succeeded(export);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StocktakeDeliveryResult.Cancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stocktake export creation failed");
                return StocktakeDeliveryResult.Failed();
            }
            finally
            {
                _operationGate.Exit();
            }
        }
    }
}
