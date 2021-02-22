using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Autofac;
using Newtonsoft.Json;
using NLog;
using QuickStockTaker.DataAccess;
using QuickStockTaker.Models;
using QuickStockTaker.ViewModels.Base;
using QuickStockTaker.Views;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    [QueryProperty(nameof(SelectedBayContent), nameof(SelectedBayContent))]
    [QueryProperty(nameof(SelectedItemContent), nameof(SelectedItemContent))]
    public class BayDetailsViewModel:BaseViewModel
    {
        #region fields
        
        private IDBConnection _dbConnection;
        
        private ObservableCollection<StocktakeItem> _unfilteredItems;

        #endregion


        #region properties

        public ObservableCollection<StocktakeItem> Items { get; set; }
        public string SearchText { get; set; }
        public int TotalQty { get; set; }
        public Bay SelectedBay { get; set; }
        public StocktakeItem SelectedItem { get; set; }


        string _selectedBayContent = "";
        public string SelectedBayContent
        {
            get => _selectedBayContent;
            set
            {
                _selectedBayContent = Uri.UnescapeDataString(value ?? string.Empty);
                OnPropertyChanged();
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
                OnPropertyChanged();
                GetSelectedItem(_selectedItemContent);
            }
        }

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

        public ICommand EditCmd => new Command<StocktakeItem>(async (item) => await OnEditCmd(item), (item) => CanNavigate);
        public ICommand SearchTextCmd => new Command(OnSearchTextCmd);
        public ICommand DeleteCmd => new Command<StocktakeItem>(async (item) => await OnDeleteCmd(item), (item) => CanNavigate);
        

        #endregion

        public BayDetailsViewModel(IUserDialogs dialogs, ILogger logger) : base(dialogs, logger)
        {
            
            _dbConnection = ViewModelLocator.Container.Resolve<IDBConnection>();

            Items = new ObservableCollection<StocktakeItem>();
        }

        private async Task OnEditCmd(StocktakeItem item)
        {
            CanNavigate = false;
            SelectedItem = item;
            // go to item details page in modal model
            var jsonStr = JsonConvert.SerializeObject(item);
            await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?Content={jsonStr}");
            CanNavigate = true;

        }


        private async Task OnDeleteCmd(StocktakeItem item)
        {
            if (item == null)
            {
                await _dialogs.AlertAsync("Please select a bay first");
                return;
            }
            

            // confirm to delete
            var isConfirmed = await _dialogs.ConfirmAsync("Are you sure want to delete this item?", "Confirm Delete", "Yes", "No");
            if (!isConfirmed) return;

            
            try
            {
                CanNavigate = false;
                // remove the select item from db and Items list
                var result = await _dbConnection.Database.DeleteAsync(item);
                if (result != 0)
                {
                    Items.Remove(item);
                    _unfilteredItems.Remove(item);
                    GetTotalCount();

                    _logger.Info($"removed {item.Qty} units of {item.Barcode}");
                }


            }
            catch (Exception e)
            {
                _logger.Error(e, $"Cannot delete stocktake item {e.Message}");
                await _dialogs.AlertAsync($"Error occured while deleting stocktake item.\n{e.Message}", "Error");

            }
            finally
            {
                CanNavigate = true;
            }
        }

        private void OnSearchTextCmd()
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


        private void GetSelectedBay(string cont)
        {
            var bay = JsonConvert.DeserializeObject<Bay>(cont);
            SelectedBay = bay;
        }

        private void GetSelectedItem(string cont)
        {
            if (string.IsNullOrEmpty(cont)) return;

            //PopulateItems();
            var updatedItem = JsonConvert.DeserializeObject<StocktakeItem>(cont);

            // update list with new value
            var originalItem = _unfilteredItems.First(x => x == SelectedItem);
            originalItem.Barcode = updatedItem.Barcode;
            originalItem.Description = updatedItem.Description;
            originalItem.Qty = updatedItem.Qty;
            OnSearchTextCmd();
            OnPropertyChanged();
        }

        public void GetTotalCount()
        {
            if (_unfilteredItems == null) return;
            TotalQty = (int)_unfilteredItems.Sum(x => x.Qty);
        }

        public async Task PopulateItems()
        {
            IsBusy = true;

            _dialogs.ShowLoading("Loading data...");

            try
            {
                List<StocktakeItem> list;
                list = await _dbConnection.Database.Table<StocktakeItem>()
                    .Where(x => x.BayLocation == SelectedBay.BayLocation).OrderByDescending(x => x.Id).ToListAsync();
               
                // add to listview
                Items = _unfilteredItems = new ObservableCollection<StocktakeItem>(list);
                GetTotalCount();
            }
            catch (Exception e)
            {
                _dialogs.HideLoading();
                await _dialogs.AlertAsync($"{e.Message}", "ERROR", "OK");
            }
            finally
            {
                IsBusy = false;

            }

            _dialogs.HideLoading();



        }

        void RefreshCanExecutes()
        {
            (EditCmd as Command)?.ChangeCanExecute();
            (DeleteCmd as Command)?.ChangeCanExecute();

        }
    }
}
