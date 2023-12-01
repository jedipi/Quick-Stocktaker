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
        Routing.RegisterRoute(nameof(EnterDatePage), typeof(EnterDatePage));
        Routing.RegisterRoute(nameof(BayListPage), typeof(BayListPage));
        Routing.RegisterRoute(nameof(BayDetailsPage), typeof(BayDetailsPage));
        Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
        Routing.RegisterRoute(nameof(ReviewPage), typeof(ReviewPage));
    }
}
