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
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class NewStocktakeViewModel : ObservableObject
    {
        #region fields
        private readonly IUserDialogs _dialogs;
        private readonly StocktakeValidator _validator;
        private readonly ILogger<NewStocktakeViewModel> _logger;
        private readonly ISQLiteRepository<StocktakeItem> _repo;
        private readonly IAppPreferences _preferences;
        private readonly INavigationService _navigationService;
        #endregion

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

        public NewStocktakeViewModel(
            IUserDialogs dialogs,
            StocktakeValidator validator,
            ILogger<NewStocktakeViewModel> logger,
            ISQLiteRepository<StocktakeItem> repo,
            IAppPreferences preferences,
            INavigationService navigationService)
        {
            Log.Information("Start NewStocktakeViewModel");
            _dialogs = dialogs;
            _validator = validator;
            _logger = logger;
            _repo = repo;
            _preferences = preferences;
            _navigationService = navigationService;
        }

        /// <summary>
        /// Save new stocktake setting
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnSave()
        {
            // validat all inputs
            var validateResult = _validator.Validate(this);
            if (!validateResult.IsValid)
            {
                await _dialogs.AlertAsync(validateResult.Errors.First().ErrorMessage, "Error", "OK");
                return;
            }

            // clear data by drop table and re-create table
            await _repo.DropandRecreateTable();

            // save values
            _preferences.Set(Constants.StocktakeNumber, StocktakeNumber);
            _preferences.Set(Constants.Site, Site);
            _preferences.Set(Constants.StocktakeDate, StocktakeDate);

            // go back to previous page
            await _navigationService.GoToAsync(NavigationRoutes.Back);
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
            var deviceId = _preferences.GetString(Constants.DeviceId, "");
            if (string.IsNullOrEmpty(deviceId))
            {
                await _dialogs.AlertAsync(
                    "Invalid Device ID. Please setup a Device ID before start a new stocktake.",
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
