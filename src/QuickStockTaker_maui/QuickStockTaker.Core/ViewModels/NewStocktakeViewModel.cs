using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Core.Data;
using Microsoft.Maui.Controls;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using Serilog;
using QuickStockTaker.Core.Validators;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Models.Sqlite;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class NewStocktakeViewModel : ObservableObject
    {
        IUserDialogs _dialogs;
        IServiceProvider _provider;
        readonly ILogger<NewStocktakeViewModel> _logger;
        ISQLiteRepository<StocktakeItem> _repo;
        #region Properties

        [ObservableProperty]
        private string _stocktakeNumber;

        [ObservableProperty]
        private string _site;

        [ObservableProperty]
        private DateTime _stocktakeDate;

        [ObservableProperty]
        private bool _isToggled;

        [ObservableProperty]
        private bool _isDetailsVisible;

        #endregion

        public NewStocktakeViewModel(IUserDialogs dialogs, 
            IServiceProvider provider, 
            ILogger<NewStocktakeViewModel> logger,
            ISQLiteRepository<StocktakeItem> repo
            ) 
        {
            Log.Information("Start NewStocktakeViewModel");
            _dialogs = dialogs;
            _provider = provider;
            _logger = logger;
            _repo = repo;
        }

        [RelayCommand]
        private async Task OnSave()
        {
            //var a = _provider.GetService<ISmtpService>();
            //var b = await a.GetSmtp("");

            var validator = _provider.GetService<StocktakeValidator>();
            var validateResult = validator.Validate(this);

            if (!validateResult.IsValid)
            {
                await _dialogs.AlertAsync(validateResult.Errors.First().ErrorMessage, "Error", "OK");
                return;
            }

            // clear data by drop table and re - create table
            await _repo.DropandRecreateTable();
           

            // save values
            Preferences.Set(Constants.StocktakeNumber, StocktakeNumber);
            Preferences.Set(Constants.Site, Site);
            Preferences.Set(Constants.StocktakeDate, StocktakeDate);

            // go back to previous page
            await Shell.Current.GoToAsync("..");
        }


        [RelayCommand]
        private async Task OnToggled()
        {
            if (!IsToggled)
            {
                IsDetailsVisible = false;
                return;
            }

            // check device id
            var deviceId = Preferences.Get("DeviceId", "");
            if ( string.IsNullOrEmpty(deviceId))
            {
                await _dialogs.AlertAsync("Invalid Device ID. Please setup a Device ID before start a new stocktake.",
                    "ERROR", "OK");
                return;
            }

            // confirm to clear data
            var clearData = await _dialogs.ConfirmAsync("Confirm to clear data", "Warning");
            if (!clearData)
            {
                IsToggled = false;
                return;
            }

            // initialise values
            StocktakeNumber = "";
            Site = "";
            StocktakeDate = DateTime.Today;


            IsDetailsVisible = true;
        }
    }
}
