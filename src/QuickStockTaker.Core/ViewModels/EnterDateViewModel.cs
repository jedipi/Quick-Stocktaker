using CommunityToolkit.Mvvm.ComponentModel;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZXing.Net.Maui;
using QuickStockTaker.Core.Popups;
using CommunityToolkit.Maui.Core;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Repositories.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;


namespace QuickStockTaker.Core.ViewModels
{
    public partial class EnterDateViewModel : ObservableObject, IRecipient<BarcodeResult[]>
    {
        #region fields
        private bool _isContinuousMode = Preferences.Get(Constants.ContinuousMode, false);
        private string _deviceId = Preferences.Get(Constants.DeviceId, "");
        private string _stocktakeNumber = Preferences.Get(Constants.StocktakeNumber, "");
        private string _site = Preferences.Get(Constants.Site, "");
        private string _stocktakeDate = Preferences.Get(Constants.StocktakeDate, DateTime.MinValue).ToShortDateString();

        private readonly IServiceProvider _provider;
        private readonly IPopupService _popupService;
        private readonly IUserDialogs _dialogs;
        private readonly ILogger<EnterDateViewModel> _logger;

        private ISQLiteRepository<StocktakeItem> _repo;
        #endregion

        #region properties
        [ObservableProperty]
        private string _bayLocation;

        [ObservableProperty]
        private ObservableCollection<StocktakeItem> _lastAddedItems;

        [ObservableProperty]
        private string _barcode;

        [ObservableProperty]
        private int _qty;

        [ObservableProperty]
        private int _bayUnits;

        [ObservableProperty]
        private bool _autoQty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ScanBarcodeCommand))]
        private bool _canScanSkuBarcode;

        #endregion

        public EnterDateViewModel(
            IUserDialogs dialogs,
            IServiceProvider provider,
            IPopupService popupService,
            ILogger<EnterDateViewModel> logger,
            ISQLiteRepository<StocktakeItem> repo)
        {
            _repo = repo;
            _logger = logger;
            _provider = provider;
            _dialogs = dialogs;
            _popupService = popupService;

            AutoQty = true;
            LastAddedItems = new ObservableCollection<StocktakeItem>();

            WeakReferenceMessenger.Default.Register(this);

        }

        // implement IRecipient to receive message from BatchDetailsViewModel
        public void Receive(BarcodeResult[] result)
        {
            Log.Information("Broadcast messaged received: {@Message}", result);
            var barcode = result.FirstOrDefault();
            Barcode = barcode.Value;

            if (AutoQty && !string.IsNullOrEmpty(Barcode))
                AddItemCommand.Execute(null);

        }

        #region Relay Commands
        [RelayCommand]
        private async Task OnAddItem()
        {
            // error : missing bay/location
            if (string.IsNullOrEmpty(BayLocation))
            {
                Vibration.Vibrate(TimeSpan.FromSeconds(2));
                await _dialogs.AlertAsync("Please enter the Bay/Location/BIN or Ref No.");
               
                return;
            }

            // error: empty barcode
            if (string.IsNullOrEmpty(Barcode))
            {
                Vibration.Vibrate(TimeSpan.FromSeconds(2));
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
            var result = await _repo.InsertAsync(item);
            if (result == 0)
            {
                await _dialogs.AlertAsync($"{Barcode} failed to save. Please try again.", "Error", "OK");
                _logger.LogWarning($"{Barcode} failed to save");
                return;
            }

            // reset barcode
            Barcode = "";

            // increase total bay qty
            BayUnits = BayUnits + Qty;

            LastAddedItems.Add(item);
            if (LastAddedItems.Count > 5)
                LastAddedItems.RemoveAt(0);

        }

        [RelayCommand]
        private async Task OnScanBayNo()
        {
            BayLocation = await Scan();
            await GetItemCount();
        }

        [RelayCommand(CanExecute = nameof(CanScanSkuBarcode))]
        private async Task OnScanBarcode()
        {
            if (_isContinuousMode)
            {
                ScanContinuously();
            }
            else
            {
                Barcode = await Scan();
                if (AutoQty && !string.IsNullOrEmpty(Barcode))
                    AddItemCommand.Execute(null);
            }
            
        }

        [RelayCommand]
        private async Task OnBayTextChanged() => await GetItemCount();

        [RelayCommand]
        private void OnClearBayNo() => BayLocation = "";

        [RelayCommand]
        private void OnClearBarCode() => Barcode = "";
        #endregion

        /// <summary>
        /// open the camera to scan
        /// </summary>
        /// <returns>barcode value</returns>
        private async Task<string> Scan()
        {
            try
            {
                var scanResults = await _popupService.ShowPopupAsync<CameraPopupViewModel>() as BarcodeResult[];
                if (scanResults == null) return null;

                var barcode = scanResults.FirstOrDefault();
                
                return barcode?.Value;
            }
            catch (Exception ex)
            {
                var a = ex.Message;
                return null;
            }
        }

        private async void ScanContinuously()
        {
            try
            {
                var scanResults = await _popupService.ShowPopupAsync<CameraPopupViewModel>(onPresenting: viewModel => { 
                    viewModel.SetIsScanContinuously(_isContinuousMode);
                }) as BarcodeResult[];
            }
            catch (Exception ex)
            {
                var a = ex.Message;
                
            }
        }

        /// <summary>
        /// Calculate the total number of scanned items for the selected bay/location.
        /// </summary>
        /// <returns></returns>
        public async Task GetItemCount()
        {
            try
            {
                var items = await _repo.GetAllAsync();
                BayUnits = (int)(items.Where(x => x.BayLocation == BayLocation).Sum(y => y.Qty));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Calculate total units failed. {e.Message}");
            }
        }
        
        partial void OnAutoQtyChanged(bool value)
        {
            // update the Qty to 1 
            if (value == true)
                Qty = 1;
        }

        partial void OnBayLocationChanged(string value)
        {
                CanScanSkuBarcode = !string.IsNullOrEmpty(value);
        }
    }
}
