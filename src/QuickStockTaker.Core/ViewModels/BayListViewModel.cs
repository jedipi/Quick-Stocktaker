using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Models;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class BayListViewModel : BaseViewModel
    {
        #region fields
        private ISQLiteRepository<StocktakeItem> _repo;
        private ObservableCollection<Bay> _unfilteredItems;
        #endregion

        #region Properties
        /// <summary>
        /// List of scanned bays
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Bay> _bays;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private int _totalQty;

        [ObservableProperty]
        private int _totalBayCount;

        [ObservableProperty]
        private Bay _selectedBay;

        #endregion
        public BayListViewModel(
            IUserDialogs dialogs,
            ILogger<BayListViewModel> logger,
            ISQLiteRepository<StocktakeItem> repo) : base(dialogs, logger)
        {
            _repo = repo;
        }

        #region RelayCommands
        /// <summary>
        /// Delete all items from the selected bay
        /// </summary>
        /// <param name="bay"></param>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnDeleteBay(Bay bay)
        {
            if (bay == null)
            {
                await _dialogs.AlertAsync("Please select a bay first");
                return;
            }

            // confirm to delete
            var result = await _dialogs.ConfirmAsync(
                $"Are you sure want to delete all data from Bay {bay.BayLocation}? \r\n\r\nWARNING: \r\nThis action cannot be undone.",
                "Confirm", "Yes", "NO");
            if (!result)
            {
                return;
            }

            var sql = "DELETE FROM StocktakeItem WHERE BayLocation = ?";
            var affectedRows = await _repo.Connection.ExecuteAsync(sql, bay.BayLocation);
            if (affectedRows == 0)
                await _dialogs.AlertAsync("No data is deleted.");
            else
            {
                _unfilteredItems.Remove(bay);
                OnSearchText();
                GetCount();

                await _dialogs.AlertAsync($"All data in Bay {bay.BayLocation} have been deleted");
                _logger.LogInformation($"Bay {bay.BayLocation}, Count: {bay.TotalCount} is deleted");

            }
        }

        /// <summary>
        /// Show all scanned item for a selected bay in the bay details page
        /// </summary>
        /// <param name="bay"></param>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnBayDetails(Bay bay)
        {
            var jsonStr = JsonSerializer.Serialize(bay);

            await Shell.Current.GoToAsync($"BayDetailsPage?SelectedBayContent={jsonStr}");
        }

        /// <summary>
        /// Update the bay number
        /// </summary>
        /// <param name="bay"></param>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnChangeBayNo(Bay bay)
        {
            var result = await Application.Current.MainPage.DisplayPromptAsync(
                 "Update Bay/Location", "Enter the new Bay/Location/Bin or Ref No:",
                "Update", "Cancel", "new Bay/Location/BIN or Ref No");

            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            var newBayLocation = result.Trim();
            // bay no cannot be empty
            if (string.IsNullOrEmpty(newBayLocation))
            {
                await _dialogs.AlertAsync("Bay/Location/Bin cannot be empty");
                return;
            }


            // check existing bay number.
            var isBayNoExist = await _repo.FindAsync(x => x.BayLocation == newBayLocation);//.CountAsync(x => x.BayLocation == newBayLocation);

            if (isBayNoExist.Any())
            {
                var isMergeBay = await _dialogs.ConfirmAsync($"Bay/Loc {newBayLocation} is already exist. \r\nDo you want to merge Bay/Loc {bay.BayLocation} into {newBayLocation}?");
                if (!isMergeBay)
                {
                    return;
                }
            }

            _dialogs.ShowLoading("Updating Bay number...");
            try
            {
                // update database
                var sql = $"UPDATE StocktakeItem SET BayLocation = ? Where BayLocation = ? ";
                await _repo.Connection.ExecuteAsync(sql, newBayLocation, bay.BayLocation);

                _logger.LogInformation($"stocktake date changed from {bay.BayLocation} to {newBayLocation}");
                

                await GetBays();
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to change bay no. {e.Message}");
                
                await _dialogs.AlertAsync("Fail to change Bay/Loc/BIN. Please try again", "ERROR");
            }
            finally
            {
                _dialogs.HideHud();
            }
        }

        [RelayCommand]
        private void OnSearchText()
        {
            var searchPhrase = SearchText?.Trim();
            if (string.IsNullOrEmpty(searchPhrase))
            {
                Bays = _unfilteredItems;
            }
            else
            {
                Bays = new ObservableCollection<Bay>(_unfilteredItems.Where(x => x.BayLocation.ToLower().Contains(searchPhrase.ToLower())));
            }
        }

        [RelayCommand]
        private async Task OnAppearing()
        {
            await GetBays();
        }

        #endregion
        public async Task GetBays()
        {
            _dialogs.ShowLoading("Loading data...");
            Bays = new ObservableCollection<Bay>();

            var items = await _repo.GetAllAsync();
            
            var bays = items.GroupBy(x => x.BayLocation).Select(l => new Bay
            {
                BayLocation = l.Key,
                TotalCount = l.Sum(y => y.Qty)
            }).OrderBy(x => x.BayLocation).ToList();

            Bays = _unfilteredItems = new ObservableCollection<Bay>(bays);

            GetCount();

            _dialogs.HideHud();

            if (_unfilteredItems.Count == 0)
            {
                await _dialogs.AlertAsync("There is no stocktake data.", "No Data", "OK");
            }
        }

        /// <summary>
        /// get total number of scanned items and total number of bays
        /// </summary>
        public void GetCount()
        {
            if (_unfilteredItems == null) return;
            TotalQty = (int)_unfilteredItems.Sum(x => x.TotalCount);
            TotalBayCount = _unfilteredItems.Count;
        }
    }
}
