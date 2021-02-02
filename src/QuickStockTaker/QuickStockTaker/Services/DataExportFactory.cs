using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using QuickStockTaker.Interfaces;
using QuickStockTaker.ViewModels.Base;

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
                    return ViewModelLocator.Container.Resolve<ICsvExport>();
                case "json":
                    return ViewModelLocator.Container.Resolve<IJsonExport>();
                default:
                    return null;

            }
        }
    }
}
