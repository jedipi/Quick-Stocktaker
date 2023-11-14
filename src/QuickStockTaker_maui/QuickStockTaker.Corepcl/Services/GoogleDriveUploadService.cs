using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Services.Interfaces;

namespace QuickStockTaker.Services
{
    /// <summary>
    /// Upload data file via Google Drive
    /// </summary>
    class GoogleDriveUploadService :IUploader
    {
        public string Name { get; }

        public Task<(bool, string)> Upload(FileInfo file)
        {
            throw new NotImplementedException();
        }
    }
}
