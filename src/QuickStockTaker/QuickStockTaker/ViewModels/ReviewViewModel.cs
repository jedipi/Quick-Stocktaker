using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Autofac;
using NLog;
using QuickStockTaker.Data;
using QuickStockTaker.DataAccess;
using QuickStockTaker.Models;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    public class ReviewViewModel:BaseViewModel
    {
        #region Fields

        
        private IDBConnection _dbConnection;

        #endregion
        

        #region Properties
        public int DeviceId => Preferences.Get(Constants.DeviceId, 0);
        public int StocktakeNumber { get; set; }
        public string Site { get; set; }
        public string StocktakeDate { get; set; }
        public int BaysCounts { get; set; }
        public int ItemCounts { get; set; }

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

        public ICommand ChangeDateCmd { get; set; }
        public ICommand ChangeSiteCmd { get; set; }
        public ICommand ChangeStocktakeNumberCmd { get; set; }

        #endregion

        public ReviewViewModel(IUserDialogs dialogs, ILogger logger, IDBConnection dbConnection) : base(dialogs, logger)
        {
            _dbConnection = dbConnection;

            StocktakeNumber = Preferences.Get(Constants.StocktakeNumber, 0);
            Site = Preferences.Get(Constants.Site, "");
            StocktakeDate = Preferences.Get(Constants.StocktakeDate, DateTime.MinValue).ToShortDateString();

            ChangeDateCmd = new Command(async ()=>await OnChangeDateCmd(), ()=>CanNavigate);
            ChangeSiteCmd = new Command(async () => await OnChangeSiteCmd(), () => CanNavigate);

        }

        public async Task OnChangeDateCmd()
        {
            CanNavigate = false;

            // keep the original data for logging
            var oldDate = StocktakeDate;

            var date = await _dialogs.DatePromptAsync("", Convert.ToDateTime(StocktakeDate));

            // selected date is the same as the date in db. Do nothing.
            if (!date.Ok || StocktakeDate == date.SelectedDate.ToShortDateString())
            {
                CanNavigate = true;
                return;
            }


            var newDate = date.SelectedDate.ToShortDateString();

            try
            {
                await _dbConnection.Database.RunInTransactionAsync((trans) =>
                    {
                        var sql = "UPDATE StocktakeItem SET StocktakeDate=?";
                        trans.Execute(sql, newDate);
                    }
                );

                Preferences.Set(Constants.StocktakeDate, date.SelectedDate);
                StocktakeDate = newDate;

                await _dialogs.AlertAsync($"stocktake date changed to {StocktakeDate}", "SUCCESS");
                
                _logger.Info($"stocktake date changed from {oldDate} to {StocktakeDate}");

            }
            catch (Exception e)
            {
                _logger.Warn($"Failed to change stocktake date. {e.Message}");
                await _dialogs.AlertAsync("Failed to change stocktake date. Please try again", "ERROR");
            }

            CanNavigate = true;
        }

        private async Task OnChangeSiteCmd()
        {
            CanNavigate = false;

            // keep the original data for logging
            var oldSite = Site;
            var result = await _dialogs.PromptAsync("Please enter the Site/Warehouse:", "Change Site/Warehouse", placeholder: "Site/Warehouse...");

            if (!result.Ok || string.IsNullOrEmpty(result.Text) || oldSite == result.Text.Trim())
            {
                CanNavigate = true;
                return;
            }

            
            var newSite = result.Text.Trim();

            try
            {
                await _dbConnection.Database.RunInTransactionAsync((trans) =>
                {
                    var sql = "UPDATE Stocktake SET Site=?";
                    trans.Execute(sql, newSite);
                }
                );

                Preferences.Set(Constants.Site, newSite);
                Site = newSite;

                await _dialogs.AlertAsync($"Store number changed to {newSite}", "SUCCESS");
                _logger.Info($"stocktake date changed from {oldSite} to {newSite}");

            }
            catch (Exception e)
            {
                _logger.Warn($"Failed to change store no. {e.Message}");
                await _dialogs.AlertAsync("Failed to change store number. Please try again", "ERROR");
            }

            CanNavigate = true;
        }

        private async Task GetCounts()
        {
            var sql = "Select BayLocation, SUM(Qty) AS TotalCount from StocktakeItem Group By BayLocation";
            var bays = await _dbConnection.Database.QueryAsync<Bay>(sql);

            if (bays == null) return;

            BaysCounts = bays.Count;
            ItemCounts = (int)bays.Sum(x => x.TotalCount);
        }

        public async Task GetReady()
        {
            _dialogs.ShowLoading("Loading Data...");
            await GetCounts();
            _dialogs.HideLoading();
        }

        private void RefreshCanExecutes()
        {
            (ChangeDateCmd as Command)?.ChangeCanExecute();
            (ChangeSiteCmd as Command)?.ChangeCanExecute();
            (ChangeStocktakeNumberCmd as Command)?.ChangeCanExecute();
        }
    }
}
