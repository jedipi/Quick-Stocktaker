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

    private void Button_Clicked(object sender, EventArgs e)
    {
#if ANDROID
        var handler = datepicker.Handler as IDatePickerHandler;
        handler.PlatformView.PerformClick();
#endif
        datepicker.Focus();

    }
}