using QuickStockTaker.Views;

namespace QuickStockTaker;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		RegisterRoutes();

    }

	private void RegisterRoutes()
	{
        Routing.RegisterRoute(nameof(NewStocktakePage), typeof(NewStocktakePage));

    }
}
