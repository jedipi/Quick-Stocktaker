namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface INavigationService
    {
        Task GoToAsync(string route);

        Task GoToAsync(string route, IDictionary<string, object> parameters);
    }
}
