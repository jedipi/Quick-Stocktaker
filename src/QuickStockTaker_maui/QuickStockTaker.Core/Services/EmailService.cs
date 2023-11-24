using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SQLitePCL;

namespace QuickStockTaker.Core.Services
{
    /// <summary>
    /// Sending email via smtp
    /// </summary>
    public class EmailService
    {
        #region Fields

        //private string _username;
        //private string _password;
        //private string _host;
        //private int _port;

        #endregion

        #region Properties

        public string Username { get;set; }
        public string Password { get;set; }
        public string Host { get;set; }
        public int Port { get;set; }

        public List<string> Recipients { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsBodyHtml { get; set; }
        public string Attachment { get; set; }
        public string Response { get; private set; }

        #endregion

        public EmailService() 
        {
            Recipients = new List<string>();
        }

        //public EmailService(string username, string password, string host, int port)
        //{
        //    Username = username;
        //    Password = password;
        //    Host = host;
        //    Port = port;

        //    Recipients = new List<string>();
        //}

        public EmailService WithAttachment(string file)
        {
            Attachment = file;
            return this;
        }

        public EmailService AddSubject(string subject)
        {
            Subject = subject;
            return this;
        }

        public EmailService AddBody(string body)
        {
            Body = body;
            return this;
        }

        public EmailService SetBodyHTML(bool isBodyHtml)
        {
            IsBodyHtml = isBodyHtml;
            return this;
        }

        public EmailService AddFrom(string from)
        {
            From = from;
            return this;
        }
        public EmailService AddRecipient(string email)
        {
            Recipients.Add(email);
            return this;
        }



        public async Task SendAsync()
        {
            var mail = new MimeMessage();

            foreach (var email in Recipients)
                mail.To.Add(MailboxAddress.Parse(email));
            
            mail.From.Add(MailboxAddress.Parse(From));
            mail.Subject = Subject;

            var bodyBuilder = new BodyBuilder();
            if (IsBodyHtml)
                bodyBuilder.HtmlBody = Body;
            else
                bodyBuilder.TextBody = Body;

            bodyBuilder.Attachments.Add(Attachment);

            // create attachment for the file located at path
            //var attachment = new MimePart()
            //{
            //    Content = new MimeContent(File.OpenRead(Attachment)),
            //    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            //    ContentTransferEncoding = ContentEncoding.Base64,
            //    FileName = Path.GetFileName(Attachment)
            //};

            //byte[] data = System.IO.File.ReadAllBytes(Attachment.FullName);



            mail.Body = bodyBuilder.ToMessageBody();


            using var client = new SmtpClient();
            
            // obtain the response from server
            client.MessageSent += (sender, args) => { Response = args.Response; };
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(Host, Port);
            await client.AuthenticateAsync(Username, Password);
            await client.SendAsync(mail);
            await client.DisconnectAsync(true);
        }
    }
}
