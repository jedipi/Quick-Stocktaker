using NSubstitute;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

internal static class TestCsvExportServiceFactory
{
    public static CsvExportService Create(
        ISQLiteRepository<StocktakeItem> repository,
        IAppPreferences preferences,
        IAppFileSystem fileSystem)
    {
        var stocktake = CreateCurrentStocktake(preferences);
        var operations = Substitute.For<IStocktakeOperationsService>();
        operations.GetItemsAsync(stocktake.Id, Arg.Any<CancellationToken>())
            .Returns(async _ => (IReadOnlyList<StocktakeItem>)await repository.GetAllAsync());

        return new CsvExportService(operations, preferences, fileSystem);
    }

    public static IStocktakeWorkspace CreateWorkspace(IAppPreferences preferences)
    {
        var workspace = Substitute.For<IStocktakeWorkspace>();
        workspace.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(CreateCurrentStocktake(preferences));
        return workspace;
    }

    private static Stocktake CreateCurrentStocktake(IAppPreferences preferences) => new()
    {
        Id = 1,
        StocktakeNumber = preferences.GetString(Constants.StocktakeNumber, ""),
        Site = preferences.GetString(Constants.Site, ""),
        StocktakeDate = preferences.GetDateTime(Constants.StocktakeDate, DateTime.MinValue)
    };
}
