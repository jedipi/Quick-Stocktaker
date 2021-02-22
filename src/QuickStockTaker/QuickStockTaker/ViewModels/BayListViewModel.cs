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
using SQLite;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    class BayListViewModel:BaseViewModel
    {
        #region fields

        
        private IDBConnection _dbConnection;
        
        private ObservableCollection<Bay> _unfilteredItems;

        #endregion
        

        #region properties

        public ObservableCollection<Bay> Bays { get; set; }
        public string SearchText { get; set; }
        public int TotalQty { get; set; }
        public int TotalBayCount { get; set; }
        public Bay SelectedBay { get; set; }

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

        public ICommand ChangeBayNoCmd => new Command<Bay>(async (bay) => await OnChangeBayCmd(bay), (item) => CanNavigate);
        public ICommand SearchTextCmd => new Command(OnSearchTextCmd);
        public ICommand BayDetailsCmd => new Command<Bay>(async (bay) => await OnBayDetailsCmd(bay), (item) => CanNavigate);
        public ICommand DeleteBayCmd => new Command<Bay>(async (bay)=> await OnDeleteBayCmd(bay), (item) => CanNavigate);

        #endregion
        

        

        public BayListViewModel(IUserDialogs dialogs, ILogger logger) : base(dialogs, logger)
        {
           
            _dbConnection = ViewModelLocator.Container.Resolve<IDBConnection>();
        }

        private async Task OnBayDetailsCmd(Bay bay)
        {
            var jsonStr = JsonConvert.SerializeObject(bay);

            await Shell.Current.GoToAsync($"{nameof(BayDetailsPage)}?SelectedBayContent={jsonStr}");
        }

        private async Task OnChangeBayCmd(Bay bay)
        {
            CanNavigate = false;
            var result = await _dialogs.PromptAsync("Enter the new Bay/Location/Bin or Ref No:", "Update Bay/Location",
                "Update", "Cancel", "new Bay/Location/BIN or Ref No");

            if (!result.Ok)
            {
                CanNavigate = true;
                return;
            }

            var newBayLocation = result?.Text.Trim();
            // bay no cannot be empty
            if (string.IsNullOrEmpty(newBayLocation))
            {
                await _dialogs.AlertAsync("Bay/Location/Bin cannot be empty");
                CanNavigate = true;
                return;
            }

           
            // check existing bay number.
            var isBayNoExist = await _dbConnection.Database.Table<StocktakeItem>().CountAsync(x => x.BayLocation == newBayLocation);

            if (isBayNoExist > 0)
            {
                var isMergeBay = await _dialogs.ConfirmAsync($"Bay/Loc {newBayLocation} is already exist. \r\nDo you want to merge Bay/Loc {bay.BayLocation} into {newBayLocation}?");
                if (!isMergeBay)
                {
                    CanNavigate = true;
                    return;
                }
            }

            _dialogs.ShowLoading("Updating Bay number...");
            try
            {
                // update database
                await _dbConnection.Database.RunInTransactionAsync((trans) =>
                    {
                        var sql = $"UPDATE StocktakeItem SET BayLocation = ? Where BayLocation = ? ";
                        trans.Execute(sql, newBayLocation, bay.BayLocation);
                    }
                );

                _logger.Info($"stocktake date changed from {bay.BayLocation} to {newBayLocation}");
                _dialogs.HideLoading();


                await GetBays();
                //await _dialogs.AlertAsync($"Bay number changed to {newBayNo}", "SUCCESS");
            }
            catch (Exception e)
            {
                _logger.Warn($"Failed to change bay no. {e.Message}");
                _dialogs.HideLoading();
                await _dialogs.AlertAsync("Fail to change Bay/Loc/BIN. Please try again", "ERROR");
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
                Bays = _unfilteredItems;
            }
            else
            {
                Bays = new ObservableCollection<Bay>(_unfilteredItems.Where(x => x.BayLocation.ToLower().Contains(searchPhrase.ToLower())));
            }
        }

        public async Task GetBays()
        {
            _dialogs.ShowLoading("Loading data...");
            Bays = new ObservableCollection<Bay>();

            var items = await _dbConnection.Database.Table<StocktakeItem>().OrderBy(x => x.BayLocation).ToListAsync();
            var bays = items.GroupBy(x => x.BayLocation).Select(l => new Bay
            {
                BayLocation = l.Key,
                TotalCount = l.Sum(y => y.Qty)
            }).OrderBy(x => x.BayLocation).ToList();

            Bays = _unfilteredItems = new ObservableCollection<Bay>(bays);

            GetCount();

            _dialogs.HideLoading();

            if (_unfilteredItems.Count == 0)
            {
                await _dialogs.AlertAsync("There is no stocktake data.", "No Data", "OK");
            }
        }

        /// <summary>
        /// get total number of items and total number of bays
        /// </summary>
        public void GetCount()
        {
            if (_unfilteredItems == null) return;
            TotalQty = (int)_unfilteredItems.Sum(x => x.TotalCount);
            TotalBayCount = _unfilteredItems.Count;
        }

        private async Task OnDeleteBayCmd(Bay bay)
        {
            CanNavigate = false;
            if (bay == null)
            {
                await _dialogs.AlertAsync("Please select a bay first");
                CanNavigate = true;
                return;
            }

            // confirm to delete
            var result = await _dialogs.ConfirmAsync(
                $"Are you sure want to delete all data from Bay {bay.BayLocation}? \r\n\r\nWARNING: \r\nThis action cannot be undone.",
                "Confirm", "Yes", "NO");
            if (!result)
            {
                CanNavigate = true;
                return;
            }

            var sql = "DELETE FROM StocktakeItem WHERE BayLocation = ?";
            var affectedRows = await _dbConnection.Database.ExecuteAsync(sql, bay.BayLocation);
            if (affectedRows == 0)
                await _dialogs.AlertAsync("No data is deleted.");
            else
            {

                //Bays.Remove(SelectedBay);
                _unfilteredItems.Remove(bay);
                OnSearchTextCmd();
                GetCount();

                await _dialogs.AlertAsync($"All data in Bay {bay.BayLocation} have been deleted");
                _logger.Info($"Bay {bay.BayLocation}, Count: {bay.TotalCount} is deleted");
                //bay = null;
            }
            CanNavigate = true;
        }

        void RefreshCanExecutes()
        {
            (ChangeBayNoCmd as Command)?.ChangeCanExecute();
            (BayDetailsCmd as Command)?.ChangeCanExecute();
            (DeleteBayCmd as Command)?.ChangeCanExecute();
        }
    }
}
