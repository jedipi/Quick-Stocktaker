using CommunityToolkit.Mvvm.ComponentModel;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using ZXing.Net.Maui;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Services;


namespace QuickStockTaker.Core.ViewModels
{
    public partial class EnterDateViewModel : ObservableObject, IRecipient<BarcodeResult[]>
    {
        #region fields
        private const int FreeStocktakeItemLimit = 250;

        private bool _isContinuousMode;

        private readonly ICameraPopupService _cameraPopupService;
        private readonly IUserDialogs _dialogs;
        private readonly ILogger<EnterDateViewModel> _logger;
        private readonly IAppPreferences _preferences;
        private readonly IEntitlementService _entitlementService;
        private readonly INavigationService _navigationService;

        private readonly IStocktakeOperationsService _stocktakeOperations;
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
            ICameraPopupService cameraPopupService,
            ILogger<EnterDateViewModel> logger,
            IStocktakeOperationsService stocktakeOperations,
            IAppPreferences preferences,
            IEntitlementService entitlementService,
            INavigationService navigationService)
        {
            _stocktakeOperations = stocktakeOperations;
            _logger = logger;
            _dialogs = dialogs;
            _cameraPopupService = cameraPopupService;
            _preferences = preferences;
            _entitlementService = entitlementService;
            _navigationService = navigationService;

            _isContinuousMode = _preferences.GetBool(Constants.ContinuousMode, false);

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

            await _entitlementService.InitializeAsync();
            var freeItemCount = -1;
            if (!_entitlementService.IsPro)
            {
                var items = await _stocktakeOperations.GetItemsAsync();
                freeItemCount = items.Count;
                if (items.Count >= FreeStocktakeItemLimit)
                {
                    await ShowFreeItemLimitAsync();
                    return;
                }
            }

            var item = new StocktakeItem
            {
                BayLocation = BayLocation,
                Barcode = Barcode,
                Qty = Qty
            };

            // add to db
            var result = await _stocktakeOperations.AddItemAsync(item);
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

            if (_isContinuousMode && freeItemCount == FreeStocktakeItemLimit - 1)
                await ShowFreeItemLimitAsync();

        }

        private async Task ShowFreeItemLimitAsync()
        {
            await _cameraPopupService.CloseAsync();
            var upgrade = await _dialogs.ConfirmAsync(
                $"The free version supports up to {FreeStocktakeItemLimit} saved items per stocktake. Upgrade to Pro for unlimited items.",
                "Free item limit reached",
                "Upgrade",
                "Not now");
            if (upgrade)
                await _navigationService.GoToAsync($"//{NavigationRoutes.AboutPage}");
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
                if (AutoQty && !string.IsNullOrEmpty(Barcode) && !string.IsNullOrEmpty(BayLocation))
                    AddItemCommand.Execute(null);
            }

        }
        [RelayCommand]
        private async Task OnBarcodeReturn()
        {
            if (AutoQty && !string.IsNullOrEmpty(Barcode) && !string.IsNullOrEmpty(BayLocation))
                AddItemCommand.Execute(null);

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
                var popupResult = await _cameraPopupService.ShowForResultAsync(isScanContinuously: false);

                if (popupResult == null)
                    return null;

                var barcode = popupResult.Barcodes.FirstOrDefault();

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
                await _cameraPopupService.ShowAsync(_isContinuousMode);
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
                var items = await _stocktakeOperations.GetItemsAsync();
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
