using Autofac;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    /// <summary>
    /// Create data exporter
    /// </summary>
    public class DataExportFactory
    {
        public IDataExport CreateExporter(string type)
        {
            switch (type)
            {
                case "csv":
                    return ServiceLocator.Container.Resolve<Interfaces.ICsvExportService>();
                case "json":
                    return ServiceLocator.Container.Resolve<IJsonExportService>();
                default:
                    return null;

            }
        }
    }
}
