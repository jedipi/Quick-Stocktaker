using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class FtpSetingViewModel : BaseViewModel
    {
        private readonly IFtpUplodService _ftpUploader;
        private readonly IAppPreferences _preferences;
        private readonly ISecureStorageService _secureStorage;
        private readonly IPageDialogService _pageDialogService;

        public bool FtpUseSftp
        {
            get => _preferences.GetBool(Constants.FtpUseSftp, true);
            set
            {
                var previousUseSftp = _preferences.GetBool(Constants.FtpUseSftp, true);
                if (previousUseSftp == value)
                    return;

                var port = GetPortForProtocolChange(previousUseSftp, value, FtpPort);

                _preferences.Set(Constants.FtpUseSftp, value);

                if (port != FtpPort)
                    FtpPort = port;

                OnPropertyChanged(nameof(FtpPort));
            }
        }

        public string FtpHost
        {
            get => _preferences.GetString(Constants.FtpHost, "");
            set
            {
                if (_preferences.GetString(Constants.FtpHost, "") == value)
                    return;

                _preferences.Set(Constants.FtpHost, value);
                OnPropertyChanged();
            }
        }

        public string FtpPort
        {
            get => _preferences.GetString(Constants.FtpPort, FtpUseSftp ? "22" : "21");
            set
            {
                if (_preferences.GetString(Constants.FtpPort, FtpUseSftp ? "22" : "21") == value)
                    return;

                _preferences.Set(Constants.FtpPort, value);
                OnPropertyChanged();
            }
        }

        public string FtpFolder
        {
            get => _preferences.GetString(Constants.FtpFolder, "");
            set
            {
                if (_preferences.GetString(Constants.FtpFolder, "") == value)
                    return;

                _preferences.Set(Constants.FtpFolder, value);
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        private string _ftpUsername;

        [ObservableProperty]
        private string _ftpPasswordDisplay;

        public FtpSetingViewModel(
            IUserDialogs dialogs,
            IFtpUplodService ftpUploader,
            IAppPreferences preferences,
            ISecureStorageService secureStorage,
            IPageDialogService pageDialogService,
            ILogger<FtpSetingViewModel> logger) : base(dialogs, logger)
        {
            _ftpUploader = ftpUploader;
            _preferences = preferences;
            _secureStorage = secureStorage;
            _pageDialogService = pageDialogService;
            _logger.LogInformation("Start FtpSetingViewModel");
        }

        [RelayCommand]
        private async Task OnAppearing()
        {
            FtpUsername = await _secureStorage.GetAsync(Constants.FtpUsername) ?? "";
            FtpPasswordDisplay = string.IsNullOrEmpty(await _secureStorage.GetAsync(Constants.FtpPassword)) ? "" : "******";
        }

        [RelayCommand]
        private async Task OnFtpHost()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Host", "Please type in the host:");

            if (string.IsNullOrEmpty(result))
                return;

            FtpHost = result.Trim();
        }

        [RelayCommand]
        private async Task OnFtpPort()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Port", "Please type in the port:", keyboard: Keyboard.Numeric);

            if (string.IsNullOrEmpty(result))
                return;

            FtpPort = result.Trim();
        }

        [RelayCommand]
        private async Task OnFtpFolder()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Folder", "Please type in the remote folder:");

            if (string.IsNullOrEmpty(result))
                return;

            FtpFolder = result.Trim();
        }

        [RelayCommand]
        private async Task OnFtpUsername()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Username", "Please type in the username:");

            if (string.IsNullOrEmpty(result))
                return;

            FtpUsername = result.Trim();
            await _secureStorage.SetAsync(Constants.FtpUsername, FtpUsername);
        }

        [RelayCommand]
        private async Task OnFtpPassword()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Password", "Please type in the password:");

            if (string.IsNullOrEmpty(result))
                return;

            await _secureStorage.SetAsync(Constants.FtpPassword, result.Trim());
            FtpPasswordDisplay = "******";
        }

        [RelayCommand]
        private async Task OnTestConnection()
        {
            try
            {
                var tokenSource = new CancellationTokenSource();
                bool success;
                string msg;

                using (var progress = _dialogs.Progress("Testing FTP/SFTP connection...", cancelText: "Cancel", cancel: tokenSource.Cancel))
                {
                    progress.Show();
                    (success, msg) = await _ftpUploader.TestConnection(tokenSource.Token);
                }

                await _dialogs.AlertAsync(msg, success ? "Success" : "Error", "OK", success ? "ic_greentick.png" : "ic_error.png");
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync($"{ex.Message}", "ERROR", "OK");
                _logger.LogError(ex, "FTP/SFTP connection test fail");
            }
        }

        internal static string GetPortForProtocolChange(bool previousUseSftp, bool nextUseSftp, string currentPort)
        {
            var previousDefaultPort = previousUseSftp ? "22" : "21";
            var nextDefaultPort = nextUseSftp ? "22" : "21";

            return string.IsNullOrWhiteSpace(currentPort) || currentPort == previousDefaultPort
                ? nextDefaultPort
                : currentPort;
        }
    }
}
