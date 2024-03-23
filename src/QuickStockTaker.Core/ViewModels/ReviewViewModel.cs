using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Popups;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class ReviewViewModel : BaseViewModel
    {
        #region Fields

        private ISQLiteRepository<StocktakeItem> _repo;
        private IPopupService _popupService;
        #endregion

        #region Properties
        [ObservableProperty]
        private string _deviceId;

        [ObservableProperty]
        private string _stocktakeNumber;

        [ObservableProperty]
        private string _site;

        [ObservableProperty]
        private string _stocktakeDate;
        
        [ObservableProperty]
        private DateTime _selectedDate;

        [ObservableProperty]
        private int _baysCounts;


        [ObservableProperty]
        private int _itemCounts;
        #endregion

        public ReviewViewModel(
            IUserDialogs dialogs,
            IPopupService popupService,
            ILogger<ReviewViewModel> logger,
            ISQLiteRepository<StocktakeItem> repo) : base(dialogs, logger)
        {
            _repo = repo;
            _popupService = popupService;

            GetData();
            SetInitialDate();
        }

        #region RelayCommands
        /// <summary>
        /// Change stocktake number
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnChangeStocktakeNumber()
        {
            // keep the original data for logging
            var oldStocktakeNo = StocktakeNumber;
            var result = await Application.Current.MainPage.DisplayPromptAsync("Change Stocktake Number", "Please enter the new Stocktake Number:");

            if (result == null || string.IsNullOrEmpty(result.Trim()) || oldStocktakeNo == result)
            {
                return;
            }

            var newStocktakeNo = result.Trim();

            try
            {
                var sql = "UPDATE StocktakeItem SET StocktakeNumber=?";
                await _repo.Connection.ExecuteAsync(sql, newStocktakeNo);
               

                Preferences.Set(Constants.StocktakeNumber, newStocktakeNo);
                StocktakeNumber = newStocktakeNo;

                await _dialogs.AlertAsync($"Stocktake number changed to {newStocktakeNo}", "SUCCESS");
                _logger.LogInformation($"stocktake number changed from {oldStocktakeNo} to {newStocktakeNo}");

            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to change stocktake no. {e.Message}");
                await _dialogs.AlertAsync("Failed to change stocktake number. Please try again", "ERROR");
            }
        }

        /// <summary>
        /// Change stocktake site
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnChangeSite()
        {
            // keep the original data for logging
            var oldSite = Site;

            var result = await Application.Current.MainPage.DisplayPromptAsync("Change Site/Warehouse", "Please enter the Site/Warehouse:");

            if (result == null || string.IsNullOrEmpty(result.Trim()) || oldSite == result.Trim())
            {
                return;
            }

            var newSite = result.Trim();

            try
            {
                var sql = "UPDATE StocktakeItem SET Site=?";
                await _repo.Connection.ExecuteAsync(sql, newSite);

                Preferences.Set(Constants.Site, newSite);
                Site = newSite;

                await _dialogs.AlertAsync($"Site/Warehouse changed to {newSite}", "SUCCESS");
                _logger.LogInformation($"Site/Warehouse changed from {oldSite} to {newSite}");

            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to change store no. {e.Message}");
                await _dialogs.AlertAsync("Failed to change store number. Please try again", "ERROR");
            }
        }

        /// <summary>
        /// Change stocktake date
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        public async Task OnChangeDate()
        {
            // keep the original data for logging
            var oldDate = StocktakeDate;
            var newDate = SelectedDate.ToShortDateString();

            // nothing changed
            if (oldDate == newDate) return;

            try
            {
                var sql = "UPDATE StocktakeItem SET StocktakeDate=?";
                await _repo.Connection.ExecuteAsync(sql, newDate);

                Preferences.Set(Constants.StocktakeDate, SelectedDate);
                StocktakeDate = newDate;

                await _dialogs.AlertAsync($"stocktake date changed to {StocktakeDate}", "SUCCESS");

                _logger.LogInformation($"stocktake date changed from {oldDate} to {StocktakeDate}");

            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to change stocktake date. {e.Message}");
                await _dialogs.AlertAsync("Failed to change stocktake date. Please try again", "ERROR");
            }

        }
        #endregion

        /// <summary>
        /// Get stocktake setting from Preference
        /// </summary>
        private void GetData()
        {
            DeviceId = Preferences.Get(Constants.DeviceId, "");
            StocktakeNumber = Preferences.Get(Constants.StocktakeNumber, "");
            Site = Preferences.Get(Constants.Site, "");
            StocktakeDate = Preferences.Get(Constants.StocktakeDate, DateTime.MinValue).ToShortDateString();
        }

        private void SetInitialDate()
        {
            var stocktakeDate = Preferences.Get(Constants.StocktakeDate, DateTime.MinValue);

            if (stocktakeDate == DateTime.MinValue || stocktakeDate.ToShortDateString() == "1/1/1900")
                SelectedDate = DateTime.Now;
            else
                SelectedDate = stocktakeDate;
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            if (value == DateTime.MinValue || value.ToShortDateString() == "1/1/1900" || value.ToShortDateString() == StocktakeDate)
                return;

            ChangeDateCommand.Execute(null);
        }
    }
}
