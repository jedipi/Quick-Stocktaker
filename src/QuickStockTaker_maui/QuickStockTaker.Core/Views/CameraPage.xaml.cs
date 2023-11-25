using QuickStockTaker.Core.ViewModels;
using ZXing.Net.Maui;

namespace QuickStockTaker.Core.Views;

public partial class CameraPage : ContentPage
{
	public CameraPage()
	{
		InitializeComponent();
        //BindingContext = vm;
        cameraBarcodeReaderView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.OneDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }


    private TaskCompletionSource<BarcodeResult[]> scanTask = new TaskCompletionSource<BarcodeResult[]>();
    public Task<BarcodeResult[]> WaitForResultAsync()
    {
        return scanTask.Task;
    }

    

    protected void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        Application.Current.MainPage.Navigation.PopModalAsync();

        scanTask.TrySetResult(e.Results);
    }

}