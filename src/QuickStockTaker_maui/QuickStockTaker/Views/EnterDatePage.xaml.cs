using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using QuickStockTaker.Core.Popups;
using QuickStockTaker.Core.ViewModels;
using ZXing.Net.Maui;

namespace QuickStockTaker.Views;

public partial class EnterDatePage : ContentPage
{
    EnterDateViewModel _vm;
    public EnterDatePage(IServiceProvider provider)
	{
		InitializeComponent();
		BindingContext = provider.GetService(typeof(EnterDateViewModel));
        _vm = BindingContext as EnterDateViewModel;

    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        //_ = Task.Run(DisplayPopup);
    }

    public async Task DisplayPopup()
    {
        //var popup = new CameraPopupPage();

        //var scanResults = await this.ShowPopupAsync(popup) as BarcodeResult[];

        //var barcode = scanResults.FirstOrDefault();
        //if (barcode != null)
        //{
        //    _vm.Barcode = barcode.Value;
        //}
    }


}