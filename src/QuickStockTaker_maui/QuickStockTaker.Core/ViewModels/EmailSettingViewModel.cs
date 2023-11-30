using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class EmailSettingViewModel : BaseViewModel
    {
        #region Properties

        [ObservableProperty]
        private bool _showFrom;

        [ObservableProperty]
        private string _smtpProvider;

        [ObservableProperty]
        private string _smtpHost;

        [ObservableProperty]
        private string _smtpPort;

        [ObservableProperty]
        private string _smtpUsername;

        [ObservableProperty]
        private string _smtpPassword;

        [ObservableProperty]
        private string _smtpFrom;

        [ObservableProperty]
        private string _testEmailButtonText;

        #endregion

        public EmailSettingViewModel(IUserDialogs dialogs, ILogger<EmailSettingViewModel> logger) : base(dialogs, logger)
        {
            _logger.LogInformation("test");
        }

        #region RelayCommands
        [RelayCommand]
        private async Task OnAppearing()
        {
            SmtpProvider = Preferences.Get(Constants.SmtpProvider, "Other");
            SmtpHost = await SecureStorage.GetAsync(Constants.SmtpHost);
            SmtpPort = await SecureStorage.GetAsync(Constants.SmtpPort);
            SmtpUsername = await SecureStorage.GetAsync(Constants.SmtpUsername);
            SmtpFrom = await SecureStorage.GetAsync(Constants.SmtpFrom);
        }

        [RelayCommand]
        private void OnHostType()
        {
            var cfg = new ActionSheetConfig()
                .SetTitle("Select a email host")
                .SetMessage("Select a email host")
                .SetUseBottomSheet(false)
                .SetCancel();

            foreach (var provider in Constants.SmtpHostProviders)
            {
                cfg.Add(provider, () =>
                {
                    SmtpProvider = provider;
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

        #endregion

        partial void OnSmtpProviderChanged(string value)
        {
            if (value == "Other")
                ShowFrom = true;
            else
                ShowFrom = false;
        }
    }
}
