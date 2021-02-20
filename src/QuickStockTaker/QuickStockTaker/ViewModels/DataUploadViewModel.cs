using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Autofac;
using FluentValidation.Validators;
using NLog;
using QuickStockTaker.Data;
using QuickStockTaker.DataAccess;
using QuickStockTaker.Interfaces;
using QuickStockTaker.Services;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Essentials;
using Xamarin.Forms;
using EmailValidator = QuickStockTaker.Validators.EmailValidator;

namespace QuickStockTaker.ViewModels
{
    public class DataUploadViewModel:BaseViewModel
    {
        #region Fields

        private FileInfo _exportedFile;
        private IEmailUploader _uploader;
        #endregion
        #region Properties

        
        bool _canNavigate = true;
        public bool CanNavigate
        {
            get { return _canNavigate; }
            set
            {
                _canNavigate = value;
                OnPropertyChanged();
                RefreshCanExecutes();
            }
        }

        

        #endregion

        #region Commands

        public ICommand EmailCmd => new Command(async ()=> await OnEmailCmd(), ()=>CanNavigate);
        public ICommand FtpCmd => new Command(async ()=> await OnFtpCmd(), () => CanNavigate);
        public ICommand GoogleDriveCmd => new Command(async ()=> await OnGoogleDriveCmd(), () => CanNavigate);
        public ICommand OneDriveCmd => new Command(async () => await OnOneDriveCmd(), () => CanNavigate);
        public ICommand DropBoxCmd => new Command(async () => await OnDropBoxCmd(), () => CanNavigate);
        public ICommand ICloudCmd => new Command(async () => await OnICloudCmd(), () => CanNavigate);


        #endregion
        public DataUploadViewModel(IEmailUploader uploader, IUserDialogs dialogs, ILogger logger)
            : base(dialogs, logger)
        {
            _uploader = uploader;
        }

        /// <summary>
        /// Export stocktake data into a file
        /// </summary>
        /// <returns></returns>
        private async Task ExportData()
        {
            if (_exportedFile != null)
                return;

            var exporterFactory = ViewModelLocator.Container.Resolve<DataExportFactory>();

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

        /// <summary>
        /// Send data via email
        /// </summary>
        /// <returns></returns>
        public async Task OnEmailCmd()
        {
            CanNavigate = false;

            // ask for email address
            var result = await _dialogs.PromptAsync("", "Email Stocktake Data", placeholder: "email address");

            // validate email address
            if (!result.Ok || string.IsNullOrEmpty(result.Value.Trim()))
            {
                CanNavigate = true;
                return;
            }

            var emailAddress = result.Value.Trim();
            var validator = ViewModelLocator.Container.Resolve<EmailValidator>();
            var validateResult = validator.Validate(emailAddress);
            if (!validateResult.IsValid)
            {
                await _dialogs.AlertAsync(validateResult.Errors.First().ErrorMessage, "Error", "OK");
                CanNavigate = true;
                return;
            }

            // export data
            await ExportData();
            if (_exportedFile == null)
            {
                CanNavigate = true;
                return;
            }

            // get smtp details.
            var provider = Preferences.Get(Constants.SmtpProvider, "Other");
            var smtpService = ViewModelLocator.Container.Resolve<SmtpService>();
            var smtp = await smtpService.GetSmtp(provider);

            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();

                var config = new ProgressDialogConfig()
                {
                    Title = $"Emailing data'",
                    CancelText = "Cancel",
                    IsDeterministic = false,
                    OnCancel = tokenSource.Cancel
                };

                bool isSuccess;
                string msg;
                using (var progress = _dialogs.Progress(config))
                {
                    progress.Show();

                    // assing email address
                    _uploader.To = emailAddress;
                    _uploader.SmtpDetail = smtp;

                    // get the from email address
                    var from = await SecureStorage.GetAsync(Constants.SmtpFrom);
                    _uploader.From = provider != "Other" ? emailAddress : from;
                    
                    (isSuccess, msg) = await _uploader.Upload(_exportedFile);
                }

                await _dialogs.AlertAsync(msg);
                
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync($"{ex.Message}","ERROR", "OK");
                _logger.Error(ex, "Email data fail");
            }

            CanNavigate = true;
        }

        public async Task OnDropBoxCmd()
        {
            await _dialogs.AlertAsync("This feature is currently not available.");
        }


        public async Task OnFtpCmd()
        {
            await _dialogs.AlertAsync("This feature is currently not available.");
        }

        public async Task OnGoogleDriveCmd()
        {
            await _dialogs.AlertAsync("This feature is currently not available.");
        }

        public async Task OnOneDriveCmd()
        {
            await _dialogs.AlertAsync("This feature is currently not available.");
        }
        public async Task OnICloudCmd()
        {
            await _dialogs.AlertAsync("This feature is currently not available.");
        }

        private void RefreshCanExecutes()
        {
            (EmailCmd as Command)?.ChangeCanExecute();
            (DropBoxCmd as Command)?.ChangeCanExecute();
            (FtpCmd as Command)?.ChangeCanExecute();
            (GoogleDriveCmd as Command)?.ChangeCanExecute();
            (OneDriveCmd as Command)?.ChangeCanExecute();
            (ICloudCmd as Command)?.ChangeCanExecute();
        }
    }
}
