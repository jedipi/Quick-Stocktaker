using System;
using System.Collections.Generic;
using System.Text;
using QuickStockTaker.Models;

namespace QuickStockTaker.Interfaces
{
    /// <summary>
    /// intermediate interface for sending email
    /// </summary>
    public interface IEmailUploader:IUploader
    {
        public string To { get; set; }
        public Smtp SmtpDetail { get; set; }
        public string From { get; set; }
    }
}
