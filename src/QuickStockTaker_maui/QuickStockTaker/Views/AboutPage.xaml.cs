using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class AboutPage : ContentPage
{
	private AboutViewModel _vm;
	public AboutPage(AboutViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}