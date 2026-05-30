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

        public bool FtpUseSftp
        {
            get => Preferences.Get(Constants.FtpUseSftp, true);
            set
            {
                if (Preferences.Get(Constants.FtpUseSftp, true) == value)
                    return;

                Preferences.Set(Constants.FtpUseSftp, value);

                if (string.IsNullOrWhiteSpace(FtpPort))
                    FtpPort = value ? "22" : "21";

                OnPropertyChanged(nameof(FtpPort));
            }
        }

        public string FtpHost
        {
            get => Preferences.Get(Constants.FtpHost, "");
            set
            {
                if (Preferences.Get(Constants.FtpHost, "") == value)
                    return;

                Preferences.Set(Constants.FtpHost, value);
                OnPropertyChanged();
            }
        }

        public string FtpPort
        {
            get => Preferences.Get(Constants.FtpPort, FtpUseSftp ? "22" : "21");
            set
            {
                if (Preferences.Get(Constants.FtpPort, FtpUseSftp ? "22" : "21") == value)
                    return;

                Preferences.Set(Constants.FtpPort, value);
                OnPropertyChanged();
            }
        }

        public string FtpFolder
        {
            get => Preferences.Get(Constants.FtpFolder, "");
            set
            {
                if (Preferences.Get(Constants.FtpFolder, "") == value)
                    return;

                Preferences.Set(Constants.FtpFolder, value);
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
            ILogger<FtpSetingViewModel> logger) : base(dialogs, logger)
        {
            _ftpUploader = ftpUploader;
            _logger.LogInformation("Start FtpSetingViewModel");
        }

        [RelayCommand]
        private async Task OnAppearing()
        {
            FtpUsername = await SecureStorage.GetAsync(Constants.FtpUsername) ?? "";
            FtpPasswordDisplay = string.IsNullOrEmpty(await SecureStorage.GetAsync(Constants.FtpPassword)) ? "" : "******";
        }

        [RelayCommand]
        private async Task OnFtpHost()
        {
            var result = await Application.Current.Windows.FirstOrDefault()?.Page?.DisplayPromptAsync(
                "FTP/SFTP Host", "Please type in the host:");

            if (string.IsNullOrEmpty(result))
                return;

            FtpHost = result.Trim();
        }

        [RelayCommand]
        private async Task OnFtpPort()
        {
            var result = await Application.Current.Windows.FirstOrDefault()?.Page?.DisplayPromptAsync(
                "FTP/SFTP Port", "Please type in the port:", keyboard: Keyboard.Numeric);

            if (string.IsNullOrEmpty(result))
                return;

            FtpPort = result.Trim();
        }

        [RelayCommand]
        private async Task OnFtpFolder()
        {
            var result = await Application.Current.Windows.FirstOrDefault()?.Page?.DisplayPromptAsync(
                "FTP/SFTP Folder", "Please type in the remote folder:");

            if (string.IsNullOrEmpty(result))
                return;

            FtpFolder = result.Trim();
        }

        [RelayCommand]
        private async Task OnFtpUsername()
        {
            var result = await Application.Current.Windows.FirstOrDefault()?.Page?.DisplayPromptAsync(
                "FTP/SFTP Username", "Please type in the username:");

            if (string.IsNullOrEmpty(result))
                return;

            FtpUsername = result.Trim();
            await SecureStorage.SetAsync(Constants.FtpUsername, FtpUsername);
        }

        [RelayCommand]
        private async Task OnFtpPassword()
        {
            var result = await Application.Current.Windows.FirstOrDefault()?.Page?.DisplayPromptAsync(
                "FTP/SFTP Password", "Please type in the password:");

            if (string.IsNullOrEmpty(result))
                return;

            await SecureStorage.SetAsync(Constants.FtpPassword, result.Trim());
            FtpPasswordDisplay = "******";
        }

        [RelayCommand]
        private async Task OnTestConnection()
        {
            try
            {
                bool success;
                string msg;

                using (var progress = _dialogs.Progress("Testing FTP/SFTP connection...", cancelText: "Cancel"))
                {
                    progress.Show();
                    (success, msg) = await _ftpUploader.TestConnection();
                }

                await _dialogs.AlertAsync(msg, success ? "Success" : "Error", "OK", success ? "ic_greentick.png" : "ic_error.png");
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync($"{ex.Message}", "ERROR", "OK");
                _logger.LogError(ex, "FTP/SFTP connection test fail");
            }
        }
    }
}
