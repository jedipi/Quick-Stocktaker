
using System.Text;
using Autofac;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models;
using QuickStockTaker.Core.Services.Interfaces;
using Microsoft.Maui;
using Microsoft.Extensions.DependencyInjection;

namespace QuickStockTaker.Core.Services
{
    /// <summary>
    /// Send stocktake data via email.
    /// </summary>
    public class EmailUploadService : IEmailUploadService
    {
        //private readonly NLog.ILogger _logger;
        private readonly EmailService _emailService;
        private readonly IAppPreferences _preferences;

        public string Name { get; }
        public string To { get; set; }
        public Smtp SmtpDetail { get; set; }
        public string From { get; set; }

        public EmailUploadService(EmailService emailService, IAppPreferences preferences)
        {
            //_logger = logger;
            _emailService = emailService;
            _preferences = preferences;
            Name = nameof(EmailUploadService);
        }

        public async Task<(bool, string)> Upload(FileInfo file)
        {
            try
            {
                var deviceId = _preferences.GetString(Constants.DeviceId, "");
                var site = _preferences.GetString(Constants.Site, "");
                var stocktakeDate = _preferences.GetDateTime(Constants.StocktakeDate, DateTime.MinValue).ToShortDateString();
                var stocktakeNumber = _preferences.GetInt(Constants.StocktakeNumber, 0);

                var body = new StringBuilder();
                body.AppendLine(@"<html><body>");
                body.AppendLine(
                    $"The file <b>{file.Name}</b> included in this email contains the stocktake data for <br><br>");
                body.AppendLine($"Scanner {deviceId}<br>");
                body.AppendLine($"Stocktake number: {stocktakeNumber}<br>");
                body.AppendLine($"Site: {site}<br>");
                body.AppendLine($"Stocktake Date:{stocktakeDate}<br>");
                body.AppendLine(@"</body></html>");

                var sender = _emailService;
                sender.Username = SmtpDetail.Username;
                sender.Password = SmtpDetail.Password;
                sender.Host = SmtpDetail.Host;
                sender.Port = SmtpDetail.Port;

                //new NamedParameter("username", SmtpDetail.Username), 
                //new NamedParameter("password", SmtpDetail.Password),
                //new NamedParameter("host", SmtpDetail.Host),
                //new NamedParameter("port", SmtpDetail.Port));

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
            catch (Exception)
            {
                //_logger.Error(e, $"Fail to email stocktake data file. {e.Message}");
                return (false, "Data send fail.");
            }

        }
    }
}
