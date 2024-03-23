using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Core.Models;

namespace QuickStockTaker.Core.Repositories.Interfaces
{
    public interface ISmtpService
    {
        Task<Smtp> GetSmtp(string type);
    }
}
