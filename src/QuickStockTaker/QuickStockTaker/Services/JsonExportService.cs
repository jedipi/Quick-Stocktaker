using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Services.Interfaces;

namespace QuickStockTaker.Services
{
    /// <summary>
    /// Export stocktake data as a json file
    /// </summary>
    public class JsonExportService:IDataExport
    {
        public FileInfo ExportedFile { get; set; }

        public Task Export()
        {
            throw new NotImplementedException();
        }
    }
}
