using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using NLog;
using Org.BouncyCastle.Asn1;
using QuickStockTaker.Data;
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
        #endregion


        #region Commands

        public ICommand SmtpHostCmd { get; set; }
        public ICommand SmtpPortCmd { get; set; }
        public ICommand SmtpUsernameCmd { get; set; }
        public ICommand SmtpPasswordCmd { get; set; }
        public ICommand SmtpFromCmd { get; set; }

        public ICommand HostTypeCmd { get; set; }

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
