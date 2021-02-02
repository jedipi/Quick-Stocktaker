using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Interfaces
{
    /// <summary>
    /// intermediate interface for uploading file
    /// </summary>
    public interface IUploader
    {
        //public FileInfo File { get; set; }
        public string Name { get; }
        public Task<(bool, string)> Upload(FileInfo file);
    }
}
