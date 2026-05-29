using Autofac;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    /// <summary>
    /// Create data exporter
    /// </summary>
    public class DataExportFactory
    {
        IServiceProvider _provider;
        public DataExportFactory(IServiceProvider provider) 
        {
            _provider = provider;
        }
        public IDataExport CreateExporter(string type)
        {
            return type switch
            {
                "csv" => _provider.GetService<ICsvExportService>(),
                "json" => _provider.GetService<IJsonExportService>(),
                _ => null,
            };
        }
    }
}
