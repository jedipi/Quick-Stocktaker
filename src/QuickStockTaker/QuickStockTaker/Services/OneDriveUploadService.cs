using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Interfaces;

namespace QuickStockTaker.Services
{
    /// <summary>
    /// Upload data file via One Drive
    /// </summary>
    public class OneDriveUploadService:IUploader
    {
        //public FileInfo File { get; set; }

        public string Name { get; }

        public Task<(bool, string)> Upload(FileInfo file)
        {
            throw new NotImplementedException();
        }
    }
}
