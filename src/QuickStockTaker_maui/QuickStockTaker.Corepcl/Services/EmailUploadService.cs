
using System.Text;
using Autofac;
using QuickStockTaker.Data;
using QuickStockTaker.Models;
using QuickStockTaker.Services.Interfaces;
using Microsoft.Maui;

namespace QuickStockTaker.Services
{
    /// <summary>
    /// Send stocktake data via email.
    /// </summary>
    public class EmailUploadService:IEmailUploader
    {
        //private readonly NLog.ILogger _logger;

        public string Name { get; }
        public string To { get; set; }
        public Smtp SmtpDetail { get; set; }
        public string From { get; set; }

        public EmailUploadService()
        {
            //_logger = logger;
            Name = nameof(EmailUploadService);
        }

        public async Task<(bool, string)> Upload(FileInfo file)
        {
            try
            { 
                var deviceId = Preferences.Get(Constants.DeviceId, 0);
                var site = Preferences.Get(Constants.Site, "");
                var stocktakeDate = (Preferences.Get(Constants.StocktakeDate, DateTime.MinValue).ToShortDateString());
                var stocktakeNumber = Preferences.Get(Constants.StocktakeNumber, 0);

                var body = new StringBuilder();
                body.AppendLine(@"<html><body>");
                body.AppendLine(
                    $"The file <b>{file.Name}</b> included in this email contains the stocktake data for <br><br>");
                body.AppendLine($"Scanner {deviceId}<br>");
                body.AppendLine($"Stocktake number: {stocktakeNumber}<br>");
                body.AppendLine($"Site: {site}<br>");
                body.AppendLine($"Stocktake Date:{stocktakeDate}<br>");
                body.AppendLine(@"</body></html>");

                //var username = await SecureStorage.GetAsync("SmtpUsername");
                //var password = await SecureStorage.GetAsync("SmtpPassword");
                //var host = await SecureStorage.GetAsync("SmtpHost");
                //var port = Convert.ToInt32(await SecureStorage.GetAsync("SmtpPort"));

                var sender = ServiceLocator.Container.Resolve<EmailService>(
                    new NamedParameter("username", SmtpDetail.Username), 
                    new NamedParameter("password", SmtpDetail.Password),
                    new NamedParameter("host", SmtpDetail.Host),
                    new NamedParameter("port", SmtpDetail.Port));

                sender.AddRecipient(To)
                    .AddFrom(From)
                    .AddSubject($"[Quick Stocktaker] Data for stocktake {stocktakeNumber}, Site {site}, Device ID {deviceId}")
                    .AddBody(body.ToString())
                    .SetBodyHTML(true)
                    .WithAttachment(file.FullName);

                await sender.SendAsync();
                //_logger.Info($"Smtp server response: {sender.Response}");
                return (true, "Data send successfully.");
            }
            catch (Exception e)
            {
                //_logger.Error(e, $"Fail to email stocktake data file. {e.Message}");
                return (true, "Data send fail.");
            }

        }
    }
}
