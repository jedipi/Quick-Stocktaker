using Microsoft.Maui.Handlers;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class ReviewPage : ContentPage
{
	public ReviewPage(ReviewViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    /// <summary>
    /// This is a workaround for opening the datapicker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Clicked(object sender, EventArgs e)
    {
#if ANDROID
        var handler = datepicker.Handler as IDatePickerHandler;
        handler.PlatformView.PerformClick();
#endif

#if IOS
        datepicker.Focus();
#endif
    }
}