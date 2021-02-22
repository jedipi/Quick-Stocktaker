using System.IO;
using System.Threading.Tasks;

namespace QuickStockTaker.Services.Interfaces
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
