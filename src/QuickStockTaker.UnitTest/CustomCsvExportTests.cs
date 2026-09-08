using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.UnitTest;

public class CustomCsvExportTests
{
    [Theory]
    [InlineData(",", true)]
    [InlineData(";", true)]
    [InlineData("\t", false)]
    [InlineData("|", false)]
    public async Task CustomColumnsPreserveEscapingLeadingZeroesAndRepeatedRows(string delimiter, bool header)
    {
        var directory = Directory.CreateTempSubdirectory("qst-presets-");
        try
        {
            var item = new StocktakeItem { Barcode = "001234", Description = "Comma, semi; pipe| tab\t quote\"\nnext", Qty = 2 };
            var preset = new ExportPreset
            {
                Delimiter = delimiter,
                IncludeHeader = header,
                Columns = [new() { Field = "Description", Header = "Details" },
                    new() { Field = "Qty", Header = "Hidden", Included = false }, new() { Field = "Barcode", Header = "Code" }]
            };
            var (csv, _) = await Create(directory.FullName, preset, [item, item]);
            var export = await csv.CreateExportAsync(new Stocktake { Id = 1 }, TestContext.Current.CancellationToken);
            using var reader = new CsvReader(new StringReader(await File.ReadAllTextAsync(export.File.FullName, TestContext.Current.CancellationToken)),
                new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter, HasHeaderRecord = header });
            if (header)
            {
                Assert.True(reader.Read());
                reader.ReadHeader();
                Assert.Equal(new[] { "Details", "Code" }, reader.HeaderRecord);
            }
            for (var i = 0; i < 2; i++)
            {
                Assert.True(reader.Read());
                Assert.Equal(2, reader.Parser.Count);
                Assert.Equal(item.Description, reader.GetField(0));
                Assert.Equal("001234", reader.GetField(1));
            }
            Assert.False(reader.Read());
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Theory]
    [InlineData("yyyy-MM-dd", "2026-09-07", "2026-09-13 14:30:00")]
    [InlineData("dd/MM/yyyy", "07/09/2026", "13/09/2026 14:30:00")]
    [InlineData("MM/dd/yyyy", "09/07/2026", "09/13/2026 14:30:00")]
    public async Task FormatsAuthoritativeStocktakeDateAndTimestamp(string format, string date, string timestamp)
    {
        var directory = Directory.CreateTempSubdirectory("qst-presets-");
        try
        {
            var preset = new ExportPreset
            {
                DateFormat = format,
                Columns = [new() { Field = "StocktakeDate", Header = "Date" }, new() { Field = "InsertedAt", Header = "Time" }]
            };
            var (csv, _) = await Create(directory.FullName, preset,
                [new StocktakeItem { StocktakeDate = "unreliable old text", InsertedAt = "2026-09-13 14:30:00" }]);
            var export = await csv.CreateExportAsync(new Stocktake { Id = 1, StocktakeDate = new DateTime(2026, 9, 7) }, TestContext.Current.CancellationToken);
            Assert.Contains(date + "," + timestamp, await File.ReadAllTextAsync(export.File.FullName, TestContext.Current.CancellationToken));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Theory]
    [InlineData("03/04/2026 14:30:00")]
    [InlineData("14:30:00")]
    [InlineData("September 7 14:30")]
    [InlineData("invalid")]
    public async Task UnreliableTimestampCleansTemporaryExportAndDefaultRetainsOriginal(string value)
    {
        var directory = Directory.CreateTempSubdirectory("qst-presets-");
        try
        {
            var preset = new ExportPreset { Columns = [new() { Field = "InsertedAt", Header = "Date" }] };
            var (csv, presets) = await Create(directory.FullName, preset, [new StocktakeItem { Id = 42, InsertedAt = value }]);
            var error = await Assert.ThrowsAsync<ExportPresetException>(() => csv.CreateExportAsync(new Stocktake { Id = 1 }, TestContext.Current.CancellationToken));
            Assert.Contains("Item 42", error.Message);
            Assert.Empty(directory.GetFiles());
            await presets.SelectAsync("");
            var export = await csv.CreateExportAsync(new Stocktake { Id = 1 }, TestContext.Current.CancellationToken);
            Assert.Contains(value, await File.ReadAllTextAsync(export.File.FullName, TestContext.Current.CancellationToken));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task DeliveryWorkflowPreservesActionablePresetError()
    {
        var workspace = Substitute.For<IStocktakeWorkspace>();
        workspace.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new Stocktake { Id = 1 });
        var csv = Substitute.For<ICsvExportService>();
        csv.CreateExportAsync(Arg.Any<Stocktake>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StocktakeExport>(new ExportPresetException("Choose Default CSV.")));
        var workflow = new StocktakeDeliveryWorkflow(csv, NullLogger<StocktakeDeliveryWorkflow>.Instance, workspace);
        var result = await workflow.CreateExportAsync(TestContext.Current.CancellationToken);
        Assert.Equal(StocktakeDeliveryStatus.Failed, result.Status);
        Assert.Null(result.Export);
        Assert.Equal("Choose Default CSV.", result.Message);
    }

    private static async Task<(CsvExportService, ExportPresetService)> Create(string path, ExportPreset preset, IReadOnlyList<StocktakeItem> items)
    {
        var preferences = new TestAppPreferences();
        var entitlement = Substitute.For<IEntitlementService>();
        entitlement.IsPro.Returns(true);
        var operations = Substitute.For<IStocktakeOperationsService>();
        operations.GetItemsAsync(1, Arg.Any<CancellationToken>()).Returns(items);
        var presets = new ExportPresetService(preferences, entitlement, Substitute.For<IStocktakeWorkspace>(), operations, new ExportPresetValidator());
        await presets.SaveAsync(preset);
        await presets.SelectAsync(preset.Id);
        return (new CsvExportService(operations, preferences, new TestAppFileSystem(path), presets), presets);
    }
}
