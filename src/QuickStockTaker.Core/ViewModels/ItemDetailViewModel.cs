using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels
{
    [QueryProperty(nameof(SelectedItem), nameof(SelectedItem))]
    public partial class ItemDetailViewModel : BaseViewModel
    {
        #region fields
        private readonly IStocktakeOperationsService _stocktakeOperations;
        private readonly INavigationService _navigationService;
        private StocktakeItem _originalItem;
        #endregion

        #region properties
        [ObservableProperty]
        private StocktakeItem _selectedItem;

        #endregion

        /// <summary>
        /// Show detail information for a selected item
        /// </summary>
        /// <param name="dialogs"></param>
        /// <param name="logger"></param>
        public ItemDetailViewModel(
            IUserDialogs dialogs,
            ILogger<ItemDetailViewModel> logger,
            IStocktakeOperationsService stocktakeOperations,
            INavigationService navigationService) : base(dialogs, logger)
        {
            _stocktakeOperations = stocktakeOperations;
            _navigationService = navigationService;
        }

        #region RelayCommands

        /// <summary>
        /// Save all changes to database
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnSave()
        {

            try
            {
                SelectedItem.Id = _originalItem.Id;
                await _stocktakeOperations.UpdateItemAsync(SelectedItem);

                var jsonStr = JsonSerializer.Serialize(SelectedItem);
                var encodedJson = Uri.EscapeDataString(jsonStr);
                await _navigationService.GoToAsync(NavigationRoutes.BackWithSelectedItem(encodedJson));
            }
            catch (Exception ex)
            {
                var a = ex.Message;
            }


        }
        #endregion

        /// <summary>
        /// keep all orginal values
        /// </summary>
        /// <param name="value"></param>
        partial void OnSelectedItemChanged(StocktakeItem value)
        {
            if (_originalItem != null) return;

            _originalItem = value;
        }
    }
}
