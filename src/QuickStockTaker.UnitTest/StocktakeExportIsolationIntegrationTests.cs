using Autofac;
using FluentAssertions;
using NSubstitute;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeExportIsolationIntegrationTests
{
    [Fact]
    public async Task CoreRegistration_ResolvesScopedExporterAndUsesCapturedCurrentIdentity()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("qst-resolved-export-");
        try
        {
            var preferences = new TestAppPreferences();
            preferences.Set(Constants.DeviceId, "SCANNER-1");
            var workspace = Substitute.For<IStocktakeWorkspace>();
            workspace.GetCurrentAsync(Arg.Any<CancellationToken>())
                .Returns(new Stocktake { Id = 42, Site = "Sydney" });
            var operations = Substitute.For<IStocktakeOperationsService>();
            operations.GetItemsAsync(42, Arg.Any<CancellationToken>())
                .Returns([
                    new StocktakeItem
                    {
                        StocktakeId = 42,
                        DeviceId = "SCANNER-1",
                        StocktakeNumber = "CURRENT",
                        Site = "Sydney",
                        BayLocation = "A1",
                        Barcode = "CURRENT-ITEM",
                        Description = "",
                        Qty = 1,
                        StocktakeDate = "31/08/2026",
                        InsertedAt = "31/08/2026 9:00 AM",
                        UpdatedAt = "31/08/2026 9:00 AM"
                    }
                ]);
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(typeof(Constants).Assembly)
                .Where(type => type.Name.EndsWith("Service"))
                .AsImplementedInterfaces();
            builder.RegisterInstance(preferences).As<IAppPreferences>();
            builder.RegisterInstance(workspace);
            builder.RegisterInstance(operations);
            builder.RegisterInstance<IAppFileSystem>(
                new TestAppFileSystem(tempDirectory.FullName));
            builder.RegisterInstance(Substitute.For<IEntitlementService>());
            builder.RegisterType<QuickStockTaker.Core.Validators.ExportPresetValidator>();
            using var container = builder.Build();
            var exporter = container.Resolve<ICsvExportService>();

            var export = await exporter.CreateExportAsync(
                (await workspace.GetCurrentAsync(TestContext.Current.CancellationToken))!,
                TestContext.Current.CancellationToken);

            export.Should().NotBeNull();
            await operations.Received(1).GetItemsAsync(42, Arg.Any<CancellationToken>());
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CreateExportAsync_ExportsOnlyCurrentStocktake()
    {
        SQLitePCL.Batteries_V2.Init();
        var tempDirectory = Directory.CreateTempSubdirectory("qst-current-export-");
        var databasePath = Path.Combine(tempDirectory.FullName, "stocktakes.db3");
        var database = new SQLiteDB(databasePath);

        try
        {
            var preferences = new TestAppPreferences();
            preferences.Set(Core.Data.Constants.DeviceId, "SCANNER-1");
            IStocktakeWorkspace workspace = new StocktakeWorkspace(
                new SQLiteRepository<Stocktake>(database),
                database,
                preferences);
            IStocktakeOperationsService operations = new StocktakeOperationsService(
                new SQLiteRepository<StocktakeItem>(database),
                database,
                workspace,
                preferences);
            var first = await workspace.CreateAsync(
                new StocktakeDefinition("FIRST", "Sydney", new DateTime(2026, 8, 30)),
                cancellationToken: TestContext.Current.CancellationToken);
            await operations.AddItemAsync(
                new StocktakeItem { BayLocation = "A1", Barcode = "FIRST-ITEM", Qty = 2 },
                TestContext.Current.CancellationToken);
            await workspace.CreateAsync(
                new StocktakeDefinition("SECOND", "Melbourne", new DateTime(2026, 8, 31)),
                cancellationToken: TestContext.Current.CancellationToken);
            await operations.AddItemAsync(
                new StocktakeItem { BayLocation = "B2", Barcode = "SECOND-ITEM", Qty = 5 },
                TestContext.Current.CancellationToken);
            await workspace.SelectAsync(first.Id, TestContext.Current.CancellationToken);
            ICsvExportService exporter = new CsvExportService(
                operations,
                preferences,
                new TestAppFileSystem(tempDirectory.FullName),
                TestCsvExportServiceFactory.DefaultPresets());

            var export = await exporter.CreateExportAsync(
                (await workspace.GetCurrentAsync(TestContext.Current.CancellationToken))!,
                TestContext.Current.CancellationToken);

            var content = await File.ReadAllTextAsync(
                export!.File.FullName,
                TestContext.Current.CancellationToken);
            content.Should().Contain("FIRST-ITEM");
            content.Should().NotContain("SECOND-ITEM");
            export.File.Name.Should().StartWith("Stocktake-Sydney-SCANNER-1-");
        }
        finally
        {
            await database.Database.CloseAsync();
            tempDirectory.Delete(recursive: true);
        }
    }
}
