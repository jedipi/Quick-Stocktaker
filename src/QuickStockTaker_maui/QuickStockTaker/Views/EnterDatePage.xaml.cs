using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class EnterDatePage : ContentPage
{
	public EnterDatePage(EnterDateViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}