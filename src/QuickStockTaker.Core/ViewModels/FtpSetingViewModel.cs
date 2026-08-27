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
        private readonly IStocktakeRemoteConnectionService _connectionService;
        private readonly IStocktakeRemoteConfigurationGate _configurationGate;
        private readonly IAppPreferences _preferences;
        private readonly ISecureStorageService _secureStorage;
        private readonly IPageDialogService _pageDialogService;
        private int _useSftpChangeVersion;

        [ObservableProperty]
        private bool _ftpUseSftp;

        [ObservableProperty]
        private string _ftpHost;

        [ObservableProperty]
        private string _ftpPort;

        [ObservableProperty]
        private string _ftpFolder;

        [ObservableProperty]
        private string _ftpUsername;

        [ObservableProperty]
        private string _ftpPasswordDisplay;

        [ObservableProperty]
        private bool _isFtpConfigurationLoaded;

        [ObservableProperty]
        private bool _hasFtpConfigurationLoadError;

        public FtpSetingViewModel(
            IUserDialogs dialogs,
            IStocktakeRemoteConnectionService connectionService,
            IStocktakeRemoteConfigurationGate configurationGate,
            IAppPreferences preferences,
            ISecureStorageService secureStorage,
            IPageDialogService pageDialogService,
            ILogger<FtpSetingViewModel> logger) : base(dialogs, logger)
        {
            _connectionService = connectionService;
            _configurationGate = configurationGate;
            _preferences = preferences;
            _secureStorage = secureStorage;
            _pageDialogService = pageDialogService;
            _logger.LogInformation("Start FtpSetingViewModel");
        }

        [RelayCommand]
        private async Task OnAppearing()
        {
            IsFtpConfigurationLoaded = false;
            HasFtpConfigurationLoadError = false;
            try
            {
                var settings = await _configurationGate.RunAsync(async () =>
                {
                    var useSftp = _preferences.GetBool(Constants.FtpUseSftp, true);
                    return (
                        UseSftp: useSftp,
                        Host: _preferences.GetString(Constants.FtpHost, ""),
                        Port: _preferences.GetString(Constants.FtpPort, useSftp ? "22" : "21"),
                        Folder: _preferences.GetString(Constants.FtpFolder, ""),
                        Username: await _secureStorage.GetAsync(Constants.FtpUsername) ?? "",
                        HasPassword: !string.IsNullOrEmpty(await _secureStorage.GetAsync(Constants.FtpPassword)));
                });

                FtpUseSftp = settings.UseSftp;
                FtpHost = settings.Host;
                FtpPort = settings.Port;
                FtpFolder = settings.Folder;
                FtpUsername = settings.Username;
                FtpPasswordDisplay = settings.HasPassword ? "******" : "";
                IsFtpConfigurationLoaded = true;
            }
            catch (Exception ex)
            {
                HasFtpConfigurationLoadError = true;
                _logger.LogError(ex, "FTP/SFTP settings load failed");
                await _dialogs.AlertAsync(
                    "FTP/SFTP settings could not be loaded. Please try again.",
                    "Error",
                    "OK",
                    "ic_error.png");
            }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task OnUseSftpToggled(bool useSftp)
        {
            var changeVersion = Interlocked.Increment(ref _useSftpChangeVersion);
            var change = await _configurationGate.RunAsync(() =>
            {
                if (changeVersion != Volatile.Read(ref _useSftpChangeVersion))
                    return Task.FromResult((Applied: false, Port: string.Empty));

                var previousUseSftp = _preferences.GetBool(Constants.FtpUseSftp, true);
                var currentPort = _preferences.GetString(
                    Constants.FtpPort,
                    previousUseSftp ? "22" : "21");
                var nextPort = GetPortForProtocolChange(previousUseSftp, useSftp, currentPort);

                _preferences.Set(Constants.FtpUseSftp, useSftp);
                if (nextPort != currentPort)
                    _preferences.Set(Constants.FtpPort, nextPort);

                return Task.FromResult((Applied: true, Port: nextPort));
            });

            if (!change.Applied || changeVersion != Volatile.Read(ref _useSftpChangeVersion))
                return;

            FtpUseSftp = useSftp;
            FtpPort = change.Port;
        }

        [RelayCommand]
        private async Task OnFtpHost()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Host", "Please type in the host:", accept: "OK");

            if (string.IsNullOrEmpty(result))
                return;

            var host = result.Trim();
            await _configurationGate.RunAsync(() =>
            {
                _preferences.Set(Constants.FtpHost, host);
                return Task.CompletedTask;
            });
            FtpHost = host;
        }

        [RelayCommand]
        private async Task OnFtpPort()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Port", "Please type in the port:", accept: "OK", keyboard: Keyboard.Numeric);

            if (string.IsNullOrEmpty(result))
                return;

            var port = result.Trim();
            await _configurationGate.RunAsync(() =>
            {
                _preferences.Set(Constants.FtpPort, port);
                return Task.CompletedTask;
            });
            FtpPort = port;
        }

        [RelayCommand]
        private async Task OnFtpFolder()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Folder", "Please type in the remote folder:", accept: "OK");

            if (string.IsNullOrEmpty(result))
                return;

            var folder = result.Trim();
            await _configurationGate.RunAsync(() =>
            {
                _preferences.Set(Constants.FtpFolder, folder);
                return Task.CompletedTask;
            });
            FtpFolder = folder;
        }

        [RelayCommand]
        private async Task OnFtpUsername()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Username", "Please type in the username:", accept: "OK");

            if (string.IsNullOrEmpty(result))
                return;

            var username = result.Trim();
            await _configurationGate.RunAsync(
                () => _secureStorage.SetAsync(Constants.FtpUsername, username));
            FtpUsername = username;
        }

        [RelayCommand]
        private async Task OnFtpPassword()
        {
            var result = await _pageDialogService.DisplayPromptAsync(
                "FTP/SFTP Password", "Please type in the password:", accept: "OK");

            if (string.IsNullOrEmpty(result))
                return;

            await _configurationGate.RunAsync(
                () => _secureStorage.SetAsync(Constants.FtpPassword, result.Trim()));
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
                    (success, msg) = await _connectionService.TestConnectionAsync(tokenSource.Token);
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
