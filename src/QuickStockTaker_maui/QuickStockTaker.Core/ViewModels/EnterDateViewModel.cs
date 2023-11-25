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
using QuickStockTaker.Core.Views;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class EnterDateViewModel : ObservableObject
    {
        #region fields
        //private bool _isContinuousMode = Preferences.Get(Constants.ContinuousMode, false);
        //private string _deviceId = Preferences.Get(Constants.DeviceId, "");
        //private string _stocktakeNumber = Preferences.Get(Constants.StocktakeNumber, "");
        //private string _site = Preferences.Get(Constants.Site, "");

        //private string _stocktakeDate =Preferences.Get(Constants.StocktakeDate, DateTime.MinValue).ToShortDateString();
        //IServiceProvider _provider;

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

        public EnterDateViewModel( ) 
        {
            //_provider = provider;
        }

        [RelayCommand]
        private async Task OnScanBarcode()
        {

            var scanResults = await GetScanResultsAsync();
            var barcode = scanResults.FirstOrDefault();
            if (barcode != null)
            {
                _ = $"Barcodes: {barcode.Format} -> {barcode.Value}";
            }
        }

        public async Task<BarcodeResult[]> GetScanResultsAsync()
        {
            //var vm = _provider.GetService(typeof(CameraViewModel)) as CameraViewModel;
            var cameraPage = new CameraPage();
            await Application.Current.MainPage.Navigation.PushModalAsync(cameraPage);

            return await cameraPage.WaitForResultAsync();
        }

        partial void OnAutoQtyChanged(bool value)
        {
            // update the Qty to 1 
            if (value == true)
                Qty = 1;
        }
    }
}
