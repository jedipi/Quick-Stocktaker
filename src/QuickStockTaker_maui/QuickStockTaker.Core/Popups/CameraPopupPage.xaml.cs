using CommunityToolkit.Maui.Views;
using ZXing.Net.Maui;

namespace QuickStockTaker.Core.Popups;

public partial class CameraPopupPage : Popup
{
	public CameraPopupPage(CameraPopupViewModel vm)
	{
		InitializeComponent();

        Size = new Size(DeviceDisplay.Current.MainDisplayInfo.Width, DeviceDisplay.Current.MainDisplayInfo.Height);
		BindingContext = vm;
        cameraBarcodeReaderView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.OneDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected async void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        try { 
            await CloseAsync(e.Results); 
        } catch (Exception ex) {
            var a = ex.Message;
        }

        //scanTask.TrySetResult(e.Results);
        
    }
}