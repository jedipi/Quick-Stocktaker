using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class AboutPage : ContentPage
{
	public AboutPage(AboutViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}