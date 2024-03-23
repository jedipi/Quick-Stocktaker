using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class BayListPage : ContentPage
{
	public BayListPage(BayListViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}