using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Engines;
using Plugin.Maui.Audio;
using ZXing.Net.Maui;

namespace QuickStockTaker.Core.Popups;

public partial class CameraPopupPage : Popup
{
    ILogger<CameraPopupPage> _logger;
    private bool _isScanContinuously;

    public BarcodeResult[] BarcodeResult { get; set; }
    

    public CameraPopupViewModel ViewModel { get; set; }
    public CameraPopupPage(CameraPopupViewModel vm, ILogger<CameraPopupPage> logger)
	{
		InitializeComponent();
        Size = new Size(DeviceDisplay.Current.MainDisplayInfo.Width, DeviceDisplay.Current.MainDisplayInfo.Height);
		
        BindingContext = ViewModel = vm;
        _logger = logger;
        _isScanContinuously = vm.IsScanContinuously;

        cameraBarcodeReaderView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.OneDimensional,
            AutoRotate = true,
            Multiple = true
        };
    }

    protected async void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        try 
        {
            
            var audioPlayer = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("beep.mp3"));
            audioPlayer.Play();
            Vibration.Default.Vibrate(TimeSpan.FromSeconds(1));

            if (ViewModel.IsScanContinuously)
            {
                BarcodeResult = e.Results;
                // send a broadcast message, receiver is EnterDateViewModel
                WeakReferenceMessenger.Default.Send(BarcodeResult);

                Thread.Sleep(ViewModel.DelayBetweenContinuousScans);
                return; // this popup stay open 
            }

            await CloseAsync(e.Results); 
        } 
        catch (Exception ex) 
        {
            _logger.LogError(ex, ex.Message, ex.StackTrace);
        }
    }
}