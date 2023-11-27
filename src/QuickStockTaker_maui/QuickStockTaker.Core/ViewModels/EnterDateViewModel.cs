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
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Views;


namespace QuickStockTaker.Core.ViewModels
{
    public partial class EnterDateViewModel : ObservableObject
    {
        #region fields
        private bool _isContinuousMode = Preferences.Get(Constants.ContinuousMode, false);
        private string _deviceId = Preferences.Get(Constants.DeviceId, "");
        private string _stocktakeNumber = Preferences.Get(Constants.StocktakeNumber, "");
        private string _site = Preferences.Get(Constants.Site, "");

        private string _stocktakeDate = Preferences.Get(Constants.StocktakeDate, DateTime.MinValue).ToShortDateString();
        IServiceProvider _provider;
        private readonly IPopupService _popupService;

        #endregion

        #region properties
        [ObservableProperty]
        private string _bayLocation;

        [ObservableProperty]
        private ObservableCollection<StocktakeItem> _last5Items;

        [ObservableProperty]
        private string _barcode;

        [ObservableProperty]
        private int _qty;

        [ObservableProperty]
        private int _bayUnits;

        [ObservableProperty]
        private bool _autoQty;

        #endregion

        public EnterDateViewModel(IServiceProvider provider,IPopupService popupService) 
        {
            _provider = provider;
            _popupService = popupService;
        }

        [RelayCommand]
        private async Task OnScanBarcode()
        {
            try
            {
                List<BarcodeResult> barcodes = new List<BarcodeResult>();
                var scanResults = await _popupService.ShowPopupAsync<CameraPopupViewModel>() as BarcodeResult[];
                if (scanResults == null) return;

                barcodes = scanResults.ToList();

                var barcode = barcodes.FirstOrDefault();
                if (barcode != null)
                {
                    Barcode = barcode.Value;
                }
            }
            catch (Exception ex)
            {
                var a = ex.Message;
            }

        }

        

        partial void OnAutoQtyChanged(bool value)
        {
            // update the Qty to 1 
            if (value == true)
                Qty = 1;
        }
    }
}
