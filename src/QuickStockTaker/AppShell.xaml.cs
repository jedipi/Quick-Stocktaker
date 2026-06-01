using QuickStockTaker.Core.Services;
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
        Routing.RegisterRoute(NavigationRoutes.NewStocktakePage, typeof(NewStocktakePage));
        Routing.RegisterRoute(NavigationRoutes.EnterDatePage, typeof(EnterDatePage));
        Routing.RegisterRoute(NavigationRoutes.BayListPage, typeof(BayListPage));
        Routing.RegisterRoute(NavigationRoutes.BayDetailsPage, typeof(BayDetailsPage));
        Routing.RegisterRoute(NavigationRoutes.ItemDetailPage, typeof(ItemDetailPage));
        Routing.RegisterRoute(NavigationRoutes.ReviewPage, typeof(ReviewPage));
        Routing.RegisterRoute(NavigationRoutes.DataUploadPage, typeof(DataUploadPage));

    }
}
