using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class HomeTabPage : ContentPage
{
	public HomeTabPage(HomeTabViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}