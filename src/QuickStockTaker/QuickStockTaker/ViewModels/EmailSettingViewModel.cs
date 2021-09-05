using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Autofac;
using NLog;
using Org.BouncyCastle.Asn1;
using QuickStockTaker.Data;
using QuickStockTaker.Repositories.Interfaces;
using QuickStockTaker.Services;
using QuickStockTaker.Validators;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    public class EmailSettingViewModel:BaseViewModel
    {
        #region Fields

        

        #endregion


        #region Properties

        private string _smtpProvider;
        public string SmtpProvider
        {
            get => _smtpProvider;
            set
            {
                _smtpProvider = value;

                if (value == "Other")
                    ShowFrom = true;
                else
                    ShowFrom = false;

                OnPropertyChanged();

            }
        }
        public string SmtpHost { get; set; }
        public string SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpFrom { get; set; }
        public bool ShowFrom { get;set; }
        public string TestEmailButtonText { get; set; }
        #endregion


        #region Commands

        public ICommand SmtpHostCmd { get; set; }
        public ICommand SmtpPortCmd { get; set; }
        public ICommand SmtpUsernameCmd { get; set; }
        public ICommand SmtpPasswordCmd { get; set; }
        public ICommand SmtpFromCmd { get; set; }

        public ICommand HostTypeCmd { get; set; }
        public ICommand TestEmailCmd { get; set; }


        #endregion

        public EmailSettingViewModel(IUserDialogs dialogs, ILogger logger)
            : base(dialogs, logger)
        {

            HostTypeCmd = new Command(OnHostTypeCmd);
            SmtpHostCmd = new Command(async () => await OnSmtpHostCmd());
            SmtpPortCmd = new Command(async () => await OnSmtpPortCmd());
            SmtpUsernameCmd = new Command(async () => await OnSmtpUsernameCmd());
            SmtpPasswordCmd = new Command(async () => await OnSmtpPasswordCmd());
            SmtpFromCmd = new Command(async () => await OnSmtpFromCmd());
            TestEmailCmd = new Command(async () => await OnTestEmailCmd());
            TestEmailButtonText = $"{MaterialDesign.MaterialDesignIcons.Send} Send Test Email";
        }

        private async Task OnTestEmailCmd()
        {
            //CanNavigate = false;

            // ask for email address
            var result = await _dialogs.PromptAsync("", "Type in your email address", placeholder: "email address");

            // validate email address
            if (!result.Ok || string.IsNullOrEmpty(result.Value.Trim()))
            {
                return;
            }

            var emailAddress = result.Value.Trim();
            var validator = ViewModelLocator.Container.Resolve<EmailValidator>();
            var validateResult = validator.Validate(emailAddress);
            if (!validateResult.IsValid)
            {
                await _dialogs.AlertAsync(validateResult.Errors.First().ErrorMessage, "Error", "OK");
                return;
            }




            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();

                var config = new ProgressDialogConfig()
                {
                    Title = $"Sending test email...'",
                    CancelText = "Cancel",
                    IsDeterministic = false,
                    OnCancel = tokenSource.Cancel
                };

                string msg;
                using (var progress = _dialogs.Progress(config))
                {
                    progress.Show();



                    // get smtp details.
                    var provider = Preferences.Get(Constants.SmtpProvider, "Other");
                    var smtpService = ViewModelLocator.Container.Resolve<ISmtpRepository>();
                    var smtp = await smtpService.GetSmtp(provider);

                    // get the from email address
                    var from = await SecureStorage.GetAsync(Constants.SmtpFrom);
                    from = provider != "Other" ? emailAddress : from;

                    var sender = ViewModelLocator.Container.Resolve<EmailService>(
                        new NamedParameter("username", smtp.Username),
                        new NamedParameter("password", smtp.Password),
                        new NamedParameter("host", smtp.Host),
                        new NamedParameter("port", smtp.Port));

                    sender.AddRecipient(emailAddress)
                        .AddFrom(from)
                        .AddSubject($"[Quick Stocktaker] Test Email from Stocktake device")
                        .AddBody("This is a test email from the stocktak scanner")
                        .SetBodyHTML(true);
                    await sender.SendAsync();
                    _logger.Info($"Smtp server response: {sender.Response}");
                    await _dialogs.AlertAsync("Test email send successfully.");
                }



            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync($"{ex.Message}", "ERROR", "OK");
                _logger.Error(ex, "Send test email fail");
            }
        }

        private async Task OnSmtpFromCmd()
        {
            var result = await _dialogs.PromptAsync("Please type in the From email address:", "From");

            if (!result.Ok || string.IsNullOrEmpty(result.Text))
                return;

            SmtpFrom = result.Text.Trim();
            await SecureStorage.SetAsync(Constants.SmtpFrom, SmtpFrom);
        }

        private async Task OnSmtpPasswordCmd()
        {
            var result = await _dialogs.PromptAsync("Please type in the password:", "Password");

            if (!result.Ok || string.IsNullOrEmpty(result.Text))
                return;

            SmtpPassword = result.Text.Trim();
            await SecureStorage.SetAsync(Constants.SmtpPassword, SmtpPassword);
        }

        private async Task OnSmtpUsernameCmd()
        {
            var result = await _dialogs.PromptAsync("Please type in the username:", "Username");

            if (!result.Ok || string.IsNullOrEmpty(result.Text))
                return;

            SmtpUsername = result.Text.Trim();
            await SecureStorage.SetAsync(Constants.SmtpUsername, SmtpUsername);
        }

        private async Task OnSmtpPortCmd()
        {

            var result = await _dialogs.PromptAsync("Please type in the SMTP port:", "SMTP port", inputType:InputType.Number);

            if (!result.Ok || string.IsNullOrEmpty(result.Text))
                return;

            SmtpPort = result.Text.Trim();
            await SecureStorage.SetAsync(Constants.SmtpPort, SmtpPort);
        }

        private async Task OnSmtpHostCmd()
        {
            var result = await _dialogs.PromptAsync("Please type in the SMTP host:", "SMTP HOST");

            if (!result.Ok || string.IsNullOrEmpty(result.Text))
                return;

            SmtpHost = result.Text.Trim();
            await SecureStorage.SetAsync(Constants.SmtpHost, SmtpHost);
        }

        private void OnHostTypeCmd()
        {
            var cfg = new ActionSheetConfig()
                .SetTitle("Select a email host")
                .SetMessage("Select a email host")
                .SetUseBottomSheet(false)
                .SetCancel();

            foreach (var provider in Constants.SmtpHostProviders)
            {
                cfg.Add(provider,  () =>
                {
                    SmtpProvider = provider;
                    OnPropertyChanged();
                    //await SecureStorage.SetAsync(Constants.SmptProvider, SmtpProvider);
                    Preferences.Set(Constants.SmtpProvider, SmtpProvider);
                    if (SmtpProvider == "Gmail")
                    {
                        SmtpHost = "smtp.gmail.com";
                        SmtpPort = "587";
                    }

                    SecureStorage.SetAsync(Constants.SmtpHost, SmtpHost);
                    SecureStorage.SetAsync(Constants.SmtpPort, SmtpPort);
                });
            }

            _dialogs.ActionSheet(cfg);
        }

        public async Task Initialase()
        {
            SmtpProvider = Preferences.Get(Constants.SmtpProvider, "Other");
            SmtpHost = await SecureStorage.GetAsync(Constants.SmtpHost);
            SmtpPort = await SecureStorage.GetAsync(Constants.SmtpPort);
            SmtpUsername = await SecureStorage.GetAsync(Constants.SmtpUsername);
            SmtpFrom = await SecureStorage.GetAsync(Constants.SmtpFrom);
        }
    }
}
