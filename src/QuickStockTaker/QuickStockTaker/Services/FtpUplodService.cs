using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Services.Interfaces;

namespace QuickStockTaker.Services
{
    /// <summary>
    /// Upload data file to ftp server
    /// </summary>
    class FtpUplodService:IUploader
    {
        public string Name { get;  }

        public Task<(bool, string)> Upload(FileInfo file)
        {
            throw new NotImplementedException();
        }
    }
}
