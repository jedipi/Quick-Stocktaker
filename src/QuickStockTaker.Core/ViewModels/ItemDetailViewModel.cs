using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
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
        private ISQLiteRepository<StocktakeItem> _repo;
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
        /// <param name="repo"></param>
        public ItemDetailViewModel(
            IUserDialogs dialogs, 
            ILogger<ItemDetailViewModel> logger,
            ISQLiteRepository<StocktakeItem> repo) : base(dialogs, logger)
        {
            _repo = repo;
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
                var sql = $"UPDATE StocktakeItem SET Barcode= ?, Qty= ? Where Id= ? ";
                await _repo.Connection.ExecuteAsync(sql, SelectedItem.Barcode, SelectedItem.Qty,
                    _originalItem.Id);

                var jsonStr = JsonSerializer.Serialize(SelectedItem);
                await Shell.Current.GoToAsync($"..?SelectedItemContent={jsonStr}");
            }
            catch(Exception ex)
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
