using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Models;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels
{
    [QueryProperty(nameof(SelectedBayContent), nameof(SelectedBayContent))]
    [QueryProperty(nameof(SelectedItemContent), nameof(SelectedItemContent))]
    public partial class BayDetailsViewModel : BaseViewModel
    {
        #region fields
        private ObservableCollection<StocktakeItem> _unfilteredItems;
        private ISQLiteRepository<StocktakeItem> _repo;

        #endregion

        #region properties
        [ObservableProperty]
        private ObservableCollection<StocktakeItem> _items;
        
        [ObservableProperty] 
        private string _searchText;
        
        [ObservableProperty]
        private int _totalQty;
        
        [ObservableProperty]
        private Bay _selectedBay;
        
        [ObservableProperty]
        private StocktakeItem _selectedItem;


        string _selectedBayContent = "";
        public string SelectedBayContent
        {
            get => _selectedBayContent;
            set
            {
                _selectedBayContent = Uri.UnescapeDataString(value ?? string.Empty);
                SetProperty(ref _selectedBayContent, value);
                GetSelectedBay(_selectedBayContent);
            }
        }

        string _selectedItemContent = "";
        public string SelectedItemContent
        {
            get => _selectedItemContent;
            set
            {
                _selectedItemContent = Uri.UnescapeDataString(value ?? string.Empty);
                SetProperty(ref _selectedItemContent, value);
                GetSelectedItem(_selectedItemContent);
            }
        }

        #endregion

        public BayDetailsViewModel(
            IUserDialogs dialogs, 
            ILogger<BayDetailsViewModel> logger,
            ISQLiteRepository<StocktakeItem> repo) : base(dialogs, logger)
        {
            _repo = repo;   
        }

        #region RelayCommands
        /// <summary>
        /// Delete an item from a bay
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnDelete(StocktakeItem item)
        {
            if (item == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error","Please select a bay first", "OK");
                return;
            }

            // confirm to delete
            //var isConfirmed = await _dialogs.ConfirmAsync("Are you sure want to delete this item?", "Confirm Delete", "Yes", "No");
            var isConfirmed = await Application.Current.MainPage.DisplayAlert("Confirm Delete", "Are you sure want to delete this item?", "YES", "NO");

            if (!isConfirmed) return;

            try
            {
                // remove the select item from db and Items list
                var result = await _repo.DeleteAsync(item);
                if (result != 0)
                {
                    Items.Remove(item);
                    _unfilteredItems.Remove(item);
                    GetTotalCount();

                    _logger.LogInformation($"removed {item.Qty} units of {item.Barcode}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Cannot delete stocktake item {e.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Error occured while deleting stocktake item.\n{e.Message}", "OK");
            }
        }

        /// <summary>
        /// Go to item detail pag for editing
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnEdit(StocktakeItem item)
        {
            SelectedItem = item;

            var navigationParameter = new Dictionary<string, object>
            {
                { "SelectedItem", item }
            };
            await Shell.Current.GoToAsync($"ItemDetailPage", navigationParameter);
        }

        [RelayCommand]
        private void OnSearchText()
        {
            var searchPhrase = SearchText?.Trim();
            if (string.IsNullOrEmpty(searchPhrase))
            {
                Items = _unfilteredItems;
            }
            else
            {
                Items = new ObservableCollection<StocktakeItem>(_unfilteredItems.Where(x => x.Barcode.ToLower().Contains(searchPhrase.ToLower())));
            }
        }

        [RelayCommand]
        private async Task OnAppearing()
        {
            await PopulateItems();
        }
        #endregion

        /// <summary>
        /// Get all scanned items for a particular bay/location
        /// </summary>
        /// <returns></returns>
        public async Task PopulateItems()
        {
            _dialogs.ShowLoading("Loading data...");

            try
            {
                var all = await _repo.GetAllAsync();
                var list = all.Where(x => x.BayLocation == SelectedBay.BayLocation).OrderByDescending(x => x.Id).ToList();

                // add to listview
                Items = _unfilteredItems = new ObservableCollection<StocktakeItem>(list);
                GetTotalCount();
            }
            catch (Exception e)
            {
                _dialogs.HideHud();
                await _dialogs.AlertAsync($"{e.Message}", "ERROR", "OK");
            }

            _dialogs.HideHud();

        }

        private void GetSelectedBay(string cont)
        {
            var bay = JsonSerializer.Deserialize<Bay>(cont);
            SelectedBay = bay;
        }

        private void GetSelectedItem(string cont)
        {
            if (string.IsNullOrEmpty(cont)) return;

            //PopulateItems();
            var updatedItem = JsonSerializer.Deserialize<StocktakeItem>(cont);

            // update list with new value
            var originalItem = _unfilteredItems.First(x => x == SelectedItem);
            originalItem.Barcode = updatedItem.Barcode;
            originalItem.Description = updatedItem.Description;
            originalItem.Qty = updatedItem.Qty;
            OnSearchText();
        }

        public void GetTotalCount()
        {
            if (_unfilteredItems == null) return;
            TotalQty = (int)_unfilteredItems.Sum(x => x.Qty);
        }
    }
}
