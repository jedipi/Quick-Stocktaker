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
            switch (type)
            {
                case "csv":
                    return _provider.GetService<ICsvExportService>();
                case "json":
                    return _provider.GetService<IJsonExportService>();
                default:
                    return null;

            }
        }
    }
}
