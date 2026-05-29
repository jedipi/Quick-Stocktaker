using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using ZXing.Net.Maui;

namespace QuickStockTaker.Core.Popups;

public partial class CameraPopupPage : Popup<CameraPopupResult>
{
    private readonly ILogger<CameraPopupPage> _logger;

    public CameraPopupViewModel ViewModel { get; set; }
    public CameraPopupPage(CameraPopupViewModel vm, ILogger<CameraPopupPage> logger)
	{
		InitializeComponent();
        
        WidthRequest = DeviceDisplay.Current.MainDisplayInfo.Width;
        HeightRequest = DeviceDisplay.Current.MainDisplayInfo.Height;

        BindingContext = ViewModel = vm;
        _logger = logger;

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
                // send a broadcast message, receiver is EnterDateViewModel
                await MainThread.InvokeOnMainThreadAsync(() => WeakReferenceMessenger.Default.Send(e.Results));

                await Task.Delay(ViewModel.DelayBetweenContinuousScans);
                return; // this popup stay open 
            }

            await MainThread.InvokeOnMainThreadAsync(() => CloseAsync(new CameraPopupResult(e.Results))); 
        } 
        catch (Exception ex) 
        {
            _logger.LogError(ex, ex.Message, ex.StackTrace);
        }
    }
}

public sealed record CameraPopupResult(BarcodeResult[] Barcodes);
