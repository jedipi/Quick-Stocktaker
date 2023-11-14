using QuickStockTaker.Core.Models;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Repositories.Interfaces;

namespace QuickStockTaker.Core.Repositories
{
    /// <summary>
    /// Smtp server details
    /// </summary>
    public class SmtpService:ISmtpService
    {
        
        public async Task<Smtp> GetSmtp(string type)
        {
            //string host;
            //int port=25;


            //switch (type)
            //{
            //    case "Gmail":
            //        host = "smtp.gmail.com";
            //        port = 587;
            //        break;
            //    default:
            //        host = "Other";
            //        port = Convert.ToInt32(await SecureStorage.GetAsync(Constants.SmtpPort));
            //        break;
            //}

            var host = await SecureStorage.GetAsync(Constants.SmtpHost);
            var port = Convert.ToInt32(await SecureStorage.GetAsync(Constants.SmtpPort));
            var username = await SecureStorage.GetAsync(Constants.SmtpUsername);
            var password = await SecureStorage.GetAsync(Constants.SmtpPassword);


            return new Smtp()
            {
                Host = host,
                Port = port,
                Username = username,
                Password = password
            };
        }
    }
}
