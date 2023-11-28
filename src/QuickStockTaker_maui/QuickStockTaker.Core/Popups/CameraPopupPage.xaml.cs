using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui;

namespace QuickStockTaker.Core.Popups;

public partial class CameraPopupPage : Popup
{
    ILogger<CameraPopupPage> _logger;

    public CameraPopupPage(CameraPopupViewModel vm, ILogger<CameraPopupPage> logger)
	{
		InitializeComponent();
        Size = new Size(DeviceDisplay.Current.MainDisplayInfo.Width, DeviceDisplay.Current.MainDisplayInfo.Height);
		
        BindingContext = vm;
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
            await CloseAsync(e.Results); 
        } 
        catch (Exception ex) 
        {
            _logger.LogError(ex, ex.Message, ex.StackTrace);
        }
    }
}