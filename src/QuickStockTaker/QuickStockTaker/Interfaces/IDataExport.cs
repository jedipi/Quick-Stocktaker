using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Interfaces
{
    /// <summary>
    /// Interface for exporting data
    /// </summary>
    public interface IDataExport
    {
        public FileInfo ExportedFile { get; set; }

        public Task Export();

    }
}
