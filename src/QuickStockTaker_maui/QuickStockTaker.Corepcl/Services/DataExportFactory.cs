using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using QuickStockTaker.Services.Interfaces;

namespace QuickStockTaker.Services
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
                    return ServiceLocator.Container.Resolve<ICsvExport>();
                case "json":
                    return ServiceLocator.Container.Resolve<IJsonExport>();
                default:
                    return null;

            }
        }
    }
}
