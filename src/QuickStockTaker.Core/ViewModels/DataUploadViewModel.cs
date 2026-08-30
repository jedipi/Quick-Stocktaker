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

        private readonly IStocktakeDeliveryWorkflow _deliveryWorkflow;
        private readonly EmailValidator _emailValidator;
        private readonly IAppFileSystem _fileSystem;
        private readonly IPageDialogService _pageDialogService;
        #endregion
        public DataUploadViewModel(
            IUserDialogs dialogs,
            IStocktakeDeliveryWorkflow deliveryWorkflow,
            EmailValidator emailValidator,
            IAppFileSystem fileSystem,
            IPageDialogService pageDialogService,
            ILogger<DataUploadViewModel> logger) : base(dialogs, logger)
        {
            _deliveryWorkflow = deliveryWorkflow;
            _emailValidator = emailValidator;
            _fileSystem = fileSystem;
            _pageDialogService = pageDialogService;
        }

        #region Commands

        [RelayCommand]
        private async Task OnCsv()
        {
            var result = await _deliveryWorkflow.CreateExportAsync();
            if (result.Status is not StocktakeDeliveryStatus.Succeeded || result.Export is null)
            {
                var message = result.Message ?? result.Status switch
                {
                    StocktakeDeliveryStatus.NoStocktakeData => "No data is exported. Please try again.",
                    StocktakeDeliveryStatus.Cancelled => "Stocktake export cancelled.",
                    StocktakeDeliveryStatus.AlreadyInProgress => "Another stocktake delivery is already in progress.",
                    _ => "Stocktake export failed. Please try again."
                };

                await _dialogs.AlertAsync(message, "Error", "OK", "ic_error.png");
                return;
            }

            var exportedFile = result.Export.File;

            try
            {
                string filePath = _fileSystem.GetDownloadFilePath(exportedFile.Name);

                var config = new ActionSheetConfig()
                {
                    Message = "Data exported: " + exportedFile.Name,
                    UseBottomSheet = true,
                    Cancel = new ActionSheetOption("Cancel", () =>
                    {

                    }, "ic_error.png"),
                    Title = "CSV File",
                    Icon = "ic_csv.png",
                    Options = new ActionSheetOption[]
                    {
                        /*
                        new ActionSheetOption("Share", async () => await Share.Default.RequestAsync(new ShareFileRequest
                        {
                            Title = "Sharing file",
                            File = new ShareFile(exportedFile.FullName)
                        }), "ic_ios_share.png"),
                        */

                        new ActionSheetOption("Save", async () =>
                        {
                            File.Copy(exportedFile.FullName, filePath, true);
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
            var result = await _pageDialogService.DisplayPromptAsync("Email Stocktake Data", "Please type in your email address:", accept: "OK");

            // validate email address
            if (result == null || string.IsNullOrEmpty(result.Trim()))
            {
                return;
            }

            var emailAddress = result.Trim();
            var validateResult = _emailValidator.Validate(emailAddress);
            if (!validateResult.IsValid)
            {
                await _dialogs.AlertAsync(validateResult.Errors.First().ErrorMessage, "Error", "OK", "ic_error.png");
                return;
            }

            try
            {
                using var tokenSource = new CancellationTokenSource();
                StocktakeDeliveryResult deliveryResult;

                using (var progress = _dialogs.Progress(message: "Emailing data", cancelText: "Cancel", cancel: tokenSource.Cancel))
                {
                    _deliveryWorkflow.EmailDeliveryStarting += progress.Show;
                    try
                    {
                        deliveryResult = await _deliveryWorkflow.DeliverByEmailAsync(
                            emailAddress,
                            tokenSource.Token);
                    }
                    finally
                    {
                        _deliveryWorkflow.EmailDeliveryStarting -= progress.Show;
                    }
                }

                if (deliveryResult.Status == StocktakeDeliveryStatus.NoStocktakeData)
                {
                    await _dialogs.AlertAsync("Data export fail. Please try again.", "Error", "OK");
                    return;
                }

                if (deliveryResult.Status == StocktakeDeliveryStatus.InvalidConfiguration)
                {
                    await _dialogs.AlertAsync(deliveryResult.Message, "ERROR", "OK");
                    return;
                }

                var message = deliveryResult.Message ?? deliveryResult.Status switch
                {
                    StocktakeDeliveryStatus.Cancelled => "Email cancelled.",
                    StocktakeDeliveryStatus.AlreadyInProgress => "Another stocktake delivery is already in progress.",
                    _ => "Data send fail."
                };

                await _dialogs.AlertAsync(message);

            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync($"{ex.Message}", "ERROR", "OK");
            }
        }

        /// <summary>
        /// Upload stocktake data to the configured FTP/SFTP folder.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        public async Task OnFTP()
        {
            try
            {
                using var tokenSource = new CancellationTokenSource();
                StocktakeDeliveryResult result;

                using (var progress = _dialogs.Progress(message: "Uploading data", cancelText: "Cancel", cancel: tokenSource.Cancel))
                {
                    result = await _deliveryWorkflow.DeliverToConfiguredRemoteAsync(tokenSource.Token, progress.Show);
                }

                if (result.Status == StocktakeDeliveryStatus.NoStocktakeData)
                {
                    await _dialogs.AlertAsync("Data export fail. Please try again.", "Error", "OK");
                    return;
                }

                var success = result.Status == StocktakeDeliveryStatus.Succeeded;
                var message = result.Message ?? result.Status switch
                {
                    StocktakeDeliveryStatus.Cancelled => "Data upload cancelled.",
                    StocktakeDeliveryStatus.AlreadyInProgress => "Another stocktake delivery is already in progress.",
                    _ => "Data upload failed. Please try again."
                };

                await _dialogs.AlertAsync(
                    message,
                    success ? "Success" : "Error",
                    "OK",
                    success ? "ic_greentick.png" : "ic_error.png");
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync($"{ex.Message}", "ERROR", "OK");
            }
        }
        #endregion
    }
}
