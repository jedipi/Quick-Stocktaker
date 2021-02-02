using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Autofac;
using NLog;
using QuickStockTaker.Data;
using QuickStockTaker.DataAccess;
using QuickStockTaker.Models;
using QuickStockTaker.Services;
using QuickStockTaker.ViewModels.Base;
using QuickStockTaker.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    /// <summary>
    /// Start a new stocktake
    /// </summary>
    class NewStocktakeViewModel : BaseViewModel
    {
        #region Fields

        private IUserDialogs _dialogs;
        private readonly NLog.ILogger _logger;
        private IDBConnection _dbConnection;

        #endregion

        #region Properties

        public bool IsToggled { get; set; }
        public bool IsDetailsVisible { get; set; }
        public int StocktakeNumber { get; set; }
        public string Site { get; set; }
        public DateTime StocktakeDate { get; set; }

        #endregion

        #region Commands
        public Command SaveCommand { get; set; }
        public Command ToggleSwitchCommand { get; set; }
        #endregion
        

        public NewStocktakeViewModel(IUserDialogs dialogs, ILogger logger)
        {
            _dialogs = dialogs;
            _logger = logger;
            _dbConnection = ViewModelLocator.Container.Resolve<IDBConnection>();

            SaveCommand = new Command(async () => await OnSaveCmd());
            ToggleSwitchCommand = new Command(async ()=> await OnToggledCmd());
        }

        private async Task OnSaveCmd()
        {
            // clear data by drop table and re-create table
            await _dbConnection.Database.RunInTransactionAsync((trans) =>
                {
                    trans.DropTable<StocktakeItem>();
                    trans.CreateTable<StocktakeItem>();
                }
            );

            // save values
            Preferences.Set(Constants.StocktakeNumber, StocktakeNumber);
            Preferences.Set(Constants.Site, Site);
            Preferences.Set(Constants.StocktakeDate, StocktakeDate);

            // go back to previous page
            await Shell.Current.GoToAsync("..");
        }

        private async Task OnToggledCmd()
        {
            if (!IsToggled)
            {
                IsDetailsVisible = false;
                return;
            }

            // check device id
            var deviceId = Preferences.Get("DeviceId", 0);
            if (deviceId == 0)
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
            StocktakeNumber = 0;
            Site = "";
            StocktakeDate = DateTime.Today;


            IsDetailsVisible = true;
        }
    }
}
