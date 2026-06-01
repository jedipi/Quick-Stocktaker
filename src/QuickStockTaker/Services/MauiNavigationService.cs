using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Services;

public class MauiNavigationService : INavigationService
{
    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);

    public Task GoToAsync(string route, IDictionary<string, object> parameters) => Shell.Current.GoToAsync(route, parameters);
}
