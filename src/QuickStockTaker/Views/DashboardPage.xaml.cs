using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class DashboardPage : ContentPage
{
	public DashboardPage(DashboardViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}