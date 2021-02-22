using QuickStockTaker.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Autofac;
using Newtonsoft.Json;
using NLog;
using QuickStockTaker.DataAccess;
using QuickStockTaker.ViewModels.Base;
using SQLite;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    [QueryProperty(nameof(Content), nameof(Content))]
    public class ItemDetailViewModel : BaseViewModel
    {
        #region fields
        private IDBConnection _dbConnection;
        #endregion

        #region properties

        public StocktakeItem _originalItem;
        public StocktakeItem SelectedItem { get; set; }
        string content = "";
        public string Content
        {
            get => content;
            set
            {
                content = Uri.UnescapeDataString(value ?? string.Empty);
                OnPropertyChanged();
                PerformOperation(content);
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
                (SaveCmd as Command)?.ChangeCanExecute();
            }
        }
        #endregion

        #region Commands
        public ICommand SaveCmd => new Command(async () => await OnSaveCmd(), () => CanNavigate);

        #endregion



        public ItemDetailViewModel(IUserDialogs dialogs, ILogger logger) : base(dialogs, logger)
        {
            
            _dbConnection = ViewModelLocator.Container.Resolve<IDBConnection>();
        }

        private async Task OnSaveCmd()
        {
            // save to db
            // update database
           
            var sql = $"UPDATE StocktakeItem SET Barcode= ?, Qty= ? Where Id= ? ";
            await _dbConnection.Database.ExecuteAsync(sql, SelectedItem.Barcode, SelectedItem.Qty,
                _originalItem.Id);

            var jsonStr = JsonConvert.SerializeObject(SelectedItem);
            await Shell.Current.GoToAsync($"..?SelectedItemContent={jsonStr}");
            
            //await _dbConnection.Database.RunInTransactionAsync((trans) =>
            //    {
            //        var sql = $"UPDATE StocktakeItem SET BayLocation = ?, Qty = ? Where BayLocation = ? and Barcode = ? ";
            //        trans.Execute(sql, SelectedItem.BayLocation, SelectedItem.Qty,
            //            _originalItem.BayLocation, _originalItem.Barcode);
            //    }
            //);

            //var jsonStr = JsonConvert.SerializeObject(SelectedItem);
            //await Shell.Current.GoToAsync($"..?SelectedItemContent=updated");
        }


        private void PerformOperation(string getcont)
        {
            var item = JsonConvert.DeserializeObject<StocktakeItem>(getcont);
            SelectedItem = _originalItem = item;
        }
    }
}
