using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    internal sealed class StocktakeEmailAdapter : IStocktakeEmailAdapter
    {
        private readonly Func<StocktakeEmailMessage, StocktakeEmailConfiguration, CancellationToken, Task> _send;

        public StocktakeEmailAdapter()
            : this(SendMessageAsync)
        {
        }

        internal StocktakeEmailAdapter(
            Func<StocktakeEmailMessage, StocktakeEmailConfiguration, CancellationToken, Task> send)
        {
            _send = send;
        }

        public Task SendAsync(
            StocktakeEmailDelivery delivery,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var body = new StringBuilder();
            body.AppendLine("<html><body>");
            body.AppendLine(
                $"The file <b>{delivery.Export.File.Name}</b> included in this email contains the stocktake data for <br><br>");
            body.AppendLine($"Scanner {delivery.Content.DeviceId}<br>");
            body.AppendLine($"Stocktake number: {delivery.Content.StocktakeNumber}<br>");
            body.AppendLine($"Site: {delivery.Content.Site}<br>");
            body.AppendLine($"Stocktake Date:{delivery.Content.StocktakeDate}<br>");
            body.AppendLine("</body></html>");

            var message = new StocktakeEmailMessage(
                delivery.Recipient,
                delivery.Sender,
                $"[Quick Stocktaker] Data for stocktake {delivery.Content.StocktakeNumber}, Site {delivery.Content.Site}, Device ID {delivery.Content.DeviceId}",
                body.ToString(),
                delivery.Export.File);

            return _send(message, delivery.Configuration, cancellationToken);
        }

        private static async Task SendMessageAsync(
            StocktakeEmailMessage message,
            StocktakeEmailConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var mail = new MimeMessage();
            mail.To.Add(MailboxAddress.Parse(message.Recipient));
            mail.From.Add(MailboxAddress.Parse(message.Sender));
            mail.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message.HtmlBody
            };
            bodyBuilder.Attachments.Add(message.Attachment.FullName);
            mail.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;
            await client.ConnectAsync(
                configuration.Host,
                configuration.Port,
                SecureSocketOptions.Auto,
                cancellationToken);
            await client.AuthenticateAsync(
                configuration.Username,
                configuration.Password,
                cancellationToken);
            await client.SendAsync(mail, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
