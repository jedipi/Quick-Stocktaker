using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Models;

namespace QuickStockTaker.Repositories.Interfaces
{
    interface ISmtpRepository
    {
        Task<Smtp> GetSmtp(string type);
    }
}
