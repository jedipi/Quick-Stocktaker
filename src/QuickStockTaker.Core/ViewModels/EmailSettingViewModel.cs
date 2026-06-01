using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class EmailSettingViewModel : BaseViewModel
    {
        #region fields
        private readonly EmailValidator _emailValidator;
        private readonly ISmtpService _smtpService;
        private readonly EmailService _emailService;
        private readonly IAppPreferences _preferences;
        private readonly ISecureStorageService _secureStorage;
        private readonly IPageDialogService _pageDialogService;
        #endregion

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

        public EmailSettingViewModel(
            IUserDialogs dialogs,
            EmailValidator emailValidator,
            ISmtpService smtpService,
            EmailService emailService,
            IAppPreferences preferences,
            ISecureStorageService secureStorage,
            IPageDialogService pageDialogService,
            ILogger<EmailSettingViewModel> logger) : base(dialogs, logger)
        {
            _logger.LogInformation("test");
            _emailValidator = emailValidator;
            _smtpService = smtpService;
            _emailService = emailService;
            _preferences = preferences;
            _secureStorage = secureStorage;
            _pageDialogService = pageDialogService;
        }

        #region RelayCommands
        [RelayCommand]
        private async Task OnAppearing()
        {
            SmtpProvider = _preferences.GetString(Constants.SmtpProvider, "Other");
            SmtpHost = await _secureStorage.GetAsync(Constants.SmtpHost);
            SmtpPort = await _secureStorage.GetAsync(Constants.SmtpPort);
            SmtpUsername = await _secureStorage.GetAsync(Constants.SmtpUsername);
            SmtpFrom = await _secureStorage.GetAsync(Constants.SmtpFrom);
        }

        /// <summary>
        /// Test the email configation
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnTestEmail()
        {
            // ask for email address
            var result = await _pageDialogService.DisplayPromptAsync(
                "", "Type in your email address:", accept: "Send", placeholder: "email address", keyboard: Keyboard.Email);

            // validate email address
            if (string.IsNullOrEmpty(result))
                return;

            // validat email address
            var emailAddress = result.Trim();
            var validateResult = _emailValidator.Validate(emailAddress);
            if (!validateResult.IsValid)
            {
                await _dialogs.AlertAsync(validateResult.Errors.First().ErrorMessage, "Error", "OK");
                return;
            }


            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();

                using (var progress = _dialogs.Progress("Sending test email...", cancelText: "Cancel", cancel: tokenSource.Cancel))
                {
                    progress.Show();

                    // get smtp details.
                    var provider = _preferences.GetString(Constants.SmtpProvider, "Other");
                    var smtp = await _smtpService.GetSmtp(provider);

                    // get the from email address
                    var from = await _secureStorage.GetAsync(Constants.SmtpFrom);
                    from = provider != "Other" ? emailAddress : from;

                    var sender = _emailService;
                    sender.Username = smtp.Username;
                    sender.Password = smtp.Password;
                    sender.Host = smtp.Host;
                    sender.Port = smtp.Port;

                    sender.AddRecipient(emailAddress)
                        .AddFrom(from)
                        .AddSubject($"[Quick Stocktaker] Test Email from Stocktake device")
                        .AddBody("This is a test email from the stocktak scanner")
                        .SetBodyHTML(true);
                    await sender.SendAsync();
                    _logger.LogInformation($"Smtp server response: {sender.Response}");
                    await _dialogs.AlertAsync("Test email send successfully.");
                }
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync($"{ex.Message}", "ERROR", "OK");
                _logger.LogError(ex, "Send test email fail");
            }
        }

        [RelayCommand]
        private async Task OnSmtpFrom()
        {
            var result = await _pageDialogService.DisplayPromptAsync("From", "Please type in the From email address:");

            if (string.IsNullOrEmpty(result))
                return;

            SmtpFrom = result.Trim();
            await _secureStorage.SetAsync(Constants.SmtpFrom, SmtpFrom);
        }

        [RelayCommand]
        private async Task OnSmtpPassword()
        {
            var result = await _pageDialogService.DisplayPromptAsync("Password", "Please type in the password:");

            if (string.IsNullOrEmpty(result))
                return;

            SmtpPassword = result.Trim();
            await _secureStorage.SetAsync(Constants.SmtpPassword, SmtpPassword);
        }

        [RelayCommand]
        private async Task OnSmtpUsername()
        {
            var result = await _pageDialogService.DisplayPromptAsync("Username", "Please type in the username:");

            if (string.IsNullOrEmpty(result))
                return;

            SmtpUsername = result.Trim();
            await _secureStorage.SetAsync(Constants.SmtpUsername, SmtpUsername);
        }

        [RelayCommand]
        private async Task OnSmtpPort()
        {

            var result = await _pageDialogService.DisplayPromptAsync("SMTP port", "Please type in the SMTP port:", keyboard: Keyboard.Numeric);

            if (string.IsNullOrEmpty(result))
                return;

            SmtpPort = result.Trim();
            await _secureStorage.SetAsync(Constants.SmtpPort, SmtpPort);
        }

        [RelayCommand]
        private async Task OnSmtpHost()
        {
            //var result = await _dialogs.("Please type in the SMTP host:", "SMTP HOST");
            var result = await _pageDialogService.DisplayPromptAsync("SMTP HOST", "Please type in the SMTP host:");

            if (string.IsNullOrEmpty(result))
                return;

            SmtpHost = result.Trim();
            await _secureStorage.SetAsync(Constants.SmtpHost, SmtpHost);
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
                    _preferences.Set(Constants.SmtpProvider, SmtpProvider);
                    if (SmtpProvider == "Gmail")
                    {
                        SmtpHost = "smtp.gmail.com";
                        SmtpPort = "587";
                        _ = _secureStorage.SetAsync(Constants.SmtpHost, SmtpHost);
                        _ = _secureStorage.SetAsync(Constants.SmtpPort, SmtpPort);
                    }


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
