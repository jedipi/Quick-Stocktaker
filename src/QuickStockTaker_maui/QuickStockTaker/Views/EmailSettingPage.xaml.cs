using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class EmailSettingPage : ContentPage
{
	public EmailSettingPage(EmailSettingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
    {
        var a = "asdf";
    }
}