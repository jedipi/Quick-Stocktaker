namespace QuickStockTaker.Core.Services.Interfaces
{
    /// <summary>
    /// intermediate interface for export stocktake data in CSV format
    /// </summary>
    public interface ICsvExportService
    {
        Task<StocktakeExport> CreateExportAsync(CancellationToken cancellationToken = default);
    }
}
