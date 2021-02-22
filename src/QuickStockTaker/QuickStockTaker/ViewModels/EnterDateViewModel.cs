using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Autofac;
using MediaManager;
using NLog;
using PropertyChanged;
using QuickStockTaker.Data;
using QuickStockTaker.DataAccess;
using QuickStockTaker.Helpers;
using QuickStockTaker.Models;
using QuickStockTaker.Services;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;

namespace QuickStockTaker.ViewModels
{
    /// <summary>
    /// Scan item count
    /// </summary>
    class EnterDateViewModel:BaseViewModel
    {
#region fields
        private IMobileBarcodeScanner _scanner;
        private Queue<StocktakeItem> _last5Items;
        private IDBConnection _dbConnection;
        private bool _isContinuousMode = Preferences.Get("ContinuousMode", false);
        private int _deviceId = Preferences.Get(Constants.DeviceId, 0);
        private int _stocktakeNumber = Preferences.Get(Constants.StocktakeNumber, 0);
        private string _site = Preferences.Get(Constants.Site, "");

        private string _stocktakeDate =
            (Preferences.Get(Constants.StocktakeDate, DateTime.MinValue).ToShortDateString());
        #endregion

        #region properties

        public string BayLocation { get; set; }
        public ObservableCollection<StocktakeItem> Last5Items { get; set; }
        public string Barcode { get; set; }
        
        private bool _autoQty;
        public bool AutoQty
        {
            get => _autoQty;
            set
            {
                _autoQty = value;
                OnPropertyChanged(nameof(AutoQty));
                if (value == true)
                    Qty = 1;
            }
        }

        public int Qty { get; set; }

        public int BayUnits { get; set; }

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

        #region commands

        public ICommand StepperValueChangedCmd { get; set; }
        public ICommand ScanBayNoCmd { get; set; }
        public ICommand ScanBarcodeCmd { get; set; }
        public ICommand AddItemCmd { get; set; }
        public ICommand ClearBayNoCmd { get; set; }
        public ICommand ClearBarcodeCmd { get; set; }
        public ICommand BayTextChangedCmd { get; set; }
        public ICommand DeleteItemCmd { get; set; }
        public ICommand AppearingCmd { get; set; }

        #endregion



        public EnterDateViewModel(IMobileBarcodeScanner scanner, IUserDialogs dialogs, ILogger logger)
            :base(dialogs, logger)
        {
            _scanner = scanner;
            _dbConnection = ViewModelLocator.Container.Resolve<IDBConnection>();
            _last5Items = new Queue<StocktakeItem>();

            _scanner.TopText = "Hold camera up to barcode to scan";
            _scanner.BottomText = "Barcode will automatically scan";

            AutoQty = true;
            Last5Items = new ObservableCollection<StocktakeItem>();

            StepperValueChangedCmd = new Command(ExecuteStepperValueChangedCmd);
            ScanBayNoCmd = new Command(async ()=> await OnScanBayNoCmd(), () => CanNavigate);
            ScanBarcodeCmd = new Command(async ()=> await OnScanBarcodeCmd(), () => CanNavigate);
            AddItemCmd = new Command(async () => await OnAddItemCmd(), () => CanNavigate);
            ClearBayNoCmd = new Command(OnClearBayNoCmd);
            ClearBarcodeCmd = new Command(() => { Barcode = ""; });
            DeleteItemCmd = new Command<StocktakeItem>(async (item) => await OnDeleteItemCmd(item));
            BayTextChangedCmd = new Command(async () => await GetItemCount());
        }

        
        public async Task OnDeleteItemCmd(StocktakeItem item)
        {
            try
            {
                CanNavigate = false;
                // remove the select item from db and last 5 items list
                var result = await _dbConnection.Database.DeleteAsync(item);
                if (result != 0)
                {
                    BayUnits = BayUnits - (int) item.Qty;
                    Last5Items.Remove(item);
                    _logger.Info($"removed item {item.Barcode} with {item.Qty} units");
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Cannot delete stocktake item {e.Message}");
                await _dialogs.AlertAsync($"Error occured while deleting stocktake item.\n{e.Message}", "Error");
                //CanNavigate = true;
            }
            finally
            {
                CanNavigate = true;
            }
        }

        void RefreshCanExecutes()
        {
            (ScanBayNoCmd as Command)?.ChangeCanExecute();
            (ScanBarcodeCmd as Command)?.ChangeCanExecute();
            (AddItemCmd as Command)?.ChangeCanExecute();
        }

        private void OnClearBayNoCmd()
        {
            BayLocation = "";
        }

        private async Task OnAddItemCmd()
        {
            CanNavigate = false;

            // error : missing bay/location
            if (string.IsNullOrEmpty(BayLocation))
            {
                Vibration.Vibrate(TimeSpan.FromSeconds(2));
                await _dialogs.AlertAsync("Please enter the Bay/Location/BIN or Ref No.");
                CanNavigate = true;
                return;
            }

            // error: empty barcode
            if (string.IsNullOrEmpty(Barcode))
            {
                Vibration.Vibrate(TimeSpan.FromSeconds(2));
                CanNavigate = true;
                return;
            }

            var item = new StocktakeItem()
            {
                DeviceId = _deviceId,
                StocktakeNumber = _stocktakeNumber,
                Site = _site,
                BayLocation = BayLocation,
                Barcode = Barcode,
                Description = "",
                Qty = Qty,
                StocktakeDate = _stocktakeDate,
                InsertedAt = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}",
                UpdatedAt = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}"
            };

            // add to db
            var result = await _dbConnection.Database.InsertAsync(item);
            if (result == 0)
            {
                await _dialogs.AlertAsync($"{Barcode} failed to save. Please try again.", "Error", "OK");
                _logger.Warn($"{Barcode} failed to save");
                CanNavigate = true;
                return;
            }

            // reset barcode
            Barcode = "";
            
            // increase total bay qty by 1
            BayUnits = BayUnits + Qty;

            // add last scanned item to the queue
            //_last5Items.Enqueue(item);

            // remove the first scanned item from the queue
            //if (_last5Items.Count > 3)
            //_last5Items.Dequeue();

            //Last5Items = new ObservableCollection<StocktakeItem>(_last5Items.ToList());

            Last5Items.Add(item);
            if (Last5Items.Count > 5)
                Last5Items.RemoveAt(5);

            CanNavigate = true;
        }

        /// <summary>
        /// Scan item barcode
        /// </summary>
        /// <returns></returns>
        private async Task OnScanBarcodeCmd()
        {
            

            if (!_isContinuousMode)
            {
                CanNavigate = false;

                Barcode = await Scan();
                CanNavigate = true;

                if (AutoQty)
                    AddItemCmd.Execute(null);

                return;
            }

            // continuous scanning mode

            CanNavigate = false;
            ScanContinuously();
            CanNavigate = true;

        }

        /// <summary>
        /// Scan bay no from barcode
        /// </summary>
        /// <returns></returns>
        private async Task OnScanBayNoCmd()
        {
            var bay = await Scan();

            if (!string.IsNullOrEmpty(bay))
            {
                BayLocation = bay;
                await GetItemCount();
            }
        }

        private void ExecuteStepperValueChangedCmd(object sender)
        {
           
        }

        private void ScanContinuously()
        {
            try
            {
                var opt = new MobileBarcodeScanningOptions { DelayBetweenContinuousScans = 1500 };

                _scanner.ScanContinuously(opt, async (result) =>
                {
                    if (result != null && !string.IsNullOrEmpty(result.Text))
                    {
                        await CrossMediaManager.Current.PlayFromAssembly("beep.mp3", typeof(BaseViewModel).Assembly);
                        //Vibration.Vibrate();
                        Barcode = result.Text;
                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            AddItemCmd.Execute(null);
                        });
                        
                    }
                });
            }
            catch (Exception e)
            {
                _logger.Error(e, "Fail to scan continuously");
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await _dialogs.AlertAsync("Something wrong while scanning. Please try again.", "Error");
                });
            }
        }

        private async Task<string> Scan()
        {
            try
            {
                var result = await _scanner.Scan();
                if (result != null)
                {
                    await CrossMediaManager.Current.PlayFromAssembly("beep.mp3", typeof(BaseViewModel).Assembly);
                    //Vibration.Vibrate();
                }

                return result?.Text;
            }
            catch (Exception e)
            {
                _logger.Error(e,"Fail to scan");
                Device.BeginInvokeOnMainThread( async () => 
                {
                    Vibration.Vibrate();
                    await _dialogs.AlertAsync("Something wrong while scanning. Please try again.", "Error");
                });
                return null;
            }
        }

        public async Task GetItemCount()
        {
            try
            {
                var items = await _dbConnection.Database.Table<StocktakeItem>().ToListAsync();
                BayUnits =(int)(items.Where(x => x.BayLocation == BayLocation).Sum(y => y.Qty));
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Calculate total units failed. {e.Message}");
            }
        }
        

    }
}
