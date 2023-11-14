using System.IO;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.Services.Interfaces
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
