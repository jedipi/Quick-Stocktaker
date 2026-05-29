using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class DataUploadViewModel : BaseViewModel
    {
        #region Fields

        private FileInfo _exportedFile;
        private IEmailUploadService _uploader;
        private IServiceProvider _provider;
        #endregion
        public DataUploadViewModel(
            IUserDialogs dialogs,
            IServiceProvider provider,
            IEmailUploadService uploader,
            ILogger<DataUploadViewModel> logger) : base(dialogs, logger)
        {
            _uploader = uploader;
            _provider = provider;
        }

        #region Commands

        [RelayCommand]
        private async Task OnCsv()
        {
            await ExportData();
            if (_exportedFile == null)
            {
                await _dialogs.AlertAsync("No data is exported. Please try again.", "Error", "OK", "ic_error.png");
                return;
            }

            try
            {
                var targetDir = string.Empty;
#if ANDROID
                // Requires Storage permissions on older Android versions, or Scoped Storage APIs for Android 13+
                targetDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
#elif IOS
                targetDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif

                string filePath = Path.Combine(targetDir, _exportedFile.Name);


                var config = new ActionSheetConfig()
                {
                    Message = "Data exported: " + _exportedFile.Name,
                    UseBottomSheet = true,
                    Cancel = new ActionSheetOption("Cancel", () =>
                    {

                    }, "ic_error.png"),
                    Title = "CSV File",
                    Icon = "ic_csv.png",
                    Options = new ActionSheetOption[]
                    {
                        new ActionSheetOption("Share", async () => await Share.Default.RequestAsync(new ShareFileRequest
                        {
                            Title = "Sharing file",
                            File = new ShareFile(_exportedFile.FullName)
                        }), "ic_ios_share.png"),

                        new ActionSheetOption("Save", async () =>
                        {
                            File.Copy(_exportedFile.FullName, filePath, true);
                            await _dialogs.AlertAsync("File saved to: " + filePath, "Success", "OK", "ic_greentick.png");
                        }, "ic_download.png")
                    }
                };

                _dialogs.ActionSheet(config);

            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync(ex.Message, "Error", "OK", "ic_error.png");
            }
            

        }
        /// <summary>
        /// Send stocktake data via email
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        public async Task OnEmail()
        {

            // ask for email address
            var result = await Application.Current.Windows.FirstOrDefault()?.Page?.DisplayPromptAsync("Email Stocktake Data", "Please type in your email address:");

            // validate email address
            if (result == null || string.IsNullOrEmpty(result.Trim()))
            {
                return;
            }

            var emailAddress = result.Trim();
            var validator = _provider.GetService<EmailValidator>();
            var validateResult = validator.Validate(emailAddress);
            if (!validateResult.IsValid)
            {
                await _dialogs.AlertAsync(validateResult.Errors.First().ErrorMessage, "Error", "OK", "ic_error.png");
                return;
            }

            // export data
            await ExportData();
            if (_exportedFile == null)
            {
                return;
            }

            // get smtp details.
            var provider = Preferences.Get(Constants.SmtpProvider, "Other");
            var smtpService = _provider.GetService<ISmtpService>();
            var smtp = await smtpService.GetSmtp(provider);

            try
            {
                var tokenSource = new CancellationTokenSource();
                string msg;

                using (var progress = _dialogs.Progress(message: "Emailing data",cancelText:"Cancel", cancel:tokenSource.Cancel))
                {
                    progress.Show();

                    // assing email address
                    _uploader.To = emailAddress;
                    _uploader.SmtpDetail = smtp;

                    // get the from email address
                    var from = await SecureStorage.GetAsync(Constants.SmtpFrom);
                    _uploader.From = provider != "Other" ? emailAddress : from;

                    (_, msg) = await _uploader.Upload(_exportedFile);
                }

                await _dialogs.AlertAsync(msg);

            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync($"{ex.Message}", "ERROR", "OK");
                _logger.LogError(ex, "Email data fail");
            }
        }
        #endregion

        /// <summary>
        /// Export stocktake data into a file
        /// </summary>
        /// <returns></returns>
        private async Task ExportData()
        {
            if (_exportedFile != null)
                return;

            var exporterFactory = _provider.GetService<DataExportFactory>();

            // TODO: check what file format is needed. 
            var exporter = exporterFactory.CreateExporter("csv");
            await exporter.Export();
            if (exporter.ExportedFile == null)
            {
                await _dialogs.AlertAsync("Data export fail. Please try again.", "Error", "OK");
                return;
            }

            _exportedFile = exporter.ExportedFile;
        }
    }
}
