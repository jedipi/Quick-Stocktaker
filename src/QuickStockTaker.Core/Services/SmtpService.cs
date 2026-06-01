using QuickStockTaker.Core.Models;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Repositories
{
    /// <summary>
    /// Smtp server details
    /// </summary>
    public class SmtpService : ISmtpService
    {
        private readonly ISecureStorageService _secureStorage;

        public SmtpService(ISecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
        }

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
            //        port = Convert.ToInt32(await _secureStorage.GetAsync(Constants.SmtpPort));
            //        break;
            //}

            var host = await _secureStorage.GetAsync(Constants.SmtpHost);
            var portValue = await _secureStorage.GetAsync(Constants.SmtpPort);
            if (!int.TryParse(portValue, out var port))
                throw new InvalidOperationException("SMTP port is not configured or not valid.");

            var username = await _secureStorage.GetAsync(Constants.SmtpUsername);
            var password = await _secureStorage.GetAsync(Constants.SmtpPassword);


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
