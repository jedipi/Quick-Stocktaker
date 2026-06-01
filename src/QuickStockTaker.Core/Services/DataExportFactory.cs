using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    /// <summary>
    /// Create data exporter
    /// </summary>
    public class DataExportFactory
    {
        private readonly ICsvExportService _csvExportService;

        public DataExportFactory(ICsvExportService csvExportService)
        {
            _csvExportService = csvExportService;
        }

        public IDataExport CreateExporter(string type)
        {
            return type switch
            {
                "csv" => _csvExportService,
                "json" => null,
                _ => null,
            };
        }
    }
}
