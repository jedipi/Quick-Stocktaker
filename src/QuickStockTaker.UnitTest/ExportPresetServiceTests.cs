using NSubstitute;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.UnitTest;

public class ExportPresetServiceTests
{
    [Fact]
    public async Task PersistenceAndFreeFallbackRetainData()
    {
        var preferences = new TestAppPreferences();
        var entitlement = Substitute.For<IEntitlementService>();
        entitlement.IsPro.Returns(true);
        ExportPresetService Create() => new(preferences, entitlement, Substitute.For<IStocktakeWorkspace>(),
            Substitute.For<IStocktakeOperationsService>(), new ExportPresetValidator());
        var service = Create();
        var preset = new ExportPreset { Name = " Warehouse " };
        await service.SaveAsync(preset);
        await service.SelectAsync(preset.Id);
        Assert.Equal("Warehouse", Assert.Single(await Create().GetPresetsAsync()).Name);
        preset.Name = "Receiving";
        preset.Delimiter = "|";
        await service.SaveAsync(preset);
        service = Create();
        Assert.Equal("Receiving", Assert.Single(await service.GetPresetsAsync()).Name);
        Assert.Equal("|", (await service.GetSelectionAsync()).Preset.Delimiter);
        await Assert.ThrowsAsync<ExportPresetException>(() => service.SaveAsync(new ExportPreset { Name = " receiving " }));
        var saved = preferences.GetString(Constants.ExportPresets, "");
        entitlement.IsPro.Returns(false);
        service = Create();
        await Assert.ThrowsAsync<ExportPresetException>(() => service.SaveAsync(new ExportPreset()));
        await Assert.ThrowsAsync<ExportPresetException>(() => service.SaveAsync(preset));
        await Assert.ThrowsAsync<ExportPresetException>(() => service.SelectAsync(preset.Id));
        await Assert.ThrowsAsync<ExportPresetException>(() => service.DeleteAsync(preset.Id));
        await Assert.ThrowsAsync<ExportPresetException>(() => service.PreviewAsync(preset));
        var fallback = await service.GetSelectionAsync();
        Assert.Null(fallback.Preset);
        Assert.Contains("require Pro", fallback.Notice);
        Assert.Null((await Create().GetSelectionAsync()).Notice);
        Assert.Equal(saved, preferences.GetString(Constants.ExportPresets, ""));
        entitlement.IsPro.Returns(true);
        await service.SelectAsync(preset.Id);
        await service.DeleteAsync(preset.Id);
        Assert.Contains("deleted", (await service.GetSelectionAsync()).Notice);
    }

    [Fact]
    public async Task DefaultSelectionBypassesCorruptSavedPresets()
    {
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.ExportPresets, "broken json");
        preferences.Set(Constants.SelectedExportPreset, "missing");
        var entitlement = Substitute.For<IEntitlementService>();
        entitlement.IsPro.Returns(true);
        var service = new ExportPresetService(preferences, entitlement, Substitute.For<IStocktakeWorkspace>(),
            Substitute.For<IStocktakeOperationsService>(), new ExportPresetValidator());
        await Assert.ThrowsAsync<ExportPresetException>(() => service.GetSelectionAsync());
        await service.SelectAsync("");
        Assert.Null((await service.GetSelectionAsync()).Preset);
        Assert.Equal("broken json", preferences.GetString(Constants.ExportPresets, ""));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task PreviewUsesFiveCurrentRowsOrLabelledExamples(int count)
    {
        var workspace = Substitute.For<IStocktakeWorkspace>();
        workspace.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new Stocktake { Id = 19 });
        var operations = Substitute.For<IStocktakeOperationsService>();
        operations.GetItemsAsync(19, Arg.Any<CancellationToken>()).Returns(Enumerable.Range(1, count)
            .Select(id => new StocktakeItem { Barcode = $"ROW-{id}" }).ToArray());
        var entitlement = Substitute.For<IEntitlementService>();
        entitlement.IsPro.Returns(true);
        var service = new ExportPresetService(new TestAppPreferences(), entitlement, workspace, operations, new ExportPresetValidator());
        var preview = await service.PreviewAsync(new ExportPreset { Columns = [new() { Field = "Barcode", Header = "Code" }] });
        if (count == 0)
        {
            Assert.Contains("Example data", preview);
            Assert.Contains("001234567890", preview);
        }
        else
        {
            Assert.Contains("ROW-1", preview);
            Assert.Contains("ROW-5", preview);
            Assert.DoesNotContain("ROW-6", preview);
        }
        Assert.Single(operations.ReceivedCalls());
    }

    [Theory]
    [InlineData("blank")]
    [InlineData("reserved")]
    [InlineData("delimiter")]
    [InlineData("date")]
    [InlineData("hidden")]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("header")]
    public async Task InvalidPresetCannotOverwriteSavedData(string invalid)
    {
        var preferences = new TestAppPreferences();
        var entitlement = Substitute.For<IEntitlementService>();
        entitlement.IsPro.Returns(true);
        var service = new ExportPresetService(preferences, entitlement, Substitute.For<IStocktakeWorkspace>(),
            Substitute.For<IStocktakeOperationsService>(), new ExportPresetValidator());
        var preset = new ExportPreset();
        await service.SaveAsync(preset);
        var before = preferences.GetString(Constants.ExportPresets, "");
        switch (invalid)
        {
            case "blank": preset.Name = " "; break;
            case "reserved": preset.Name = " default csv "; break;
            case "delimiter": preset.Delimiter = "::"; break;
            case "date": preset.DateFormat = "d"; break;
            case "hidden": preset.Columns.ForEach(column => column.Included = false); break;
            case "duplicate": preset.Columns.Add(preset.Columns[0]); break;
            case "unknown": preset.Columns[0].Field = "Unknown"; break;
            case "header": preset.Columns[0].Header = " "; break;
        }
        await Assert.ThrowsAsync<ExportPresetException>(() => service.SaveAsync(preset));
        Assert.Equal(before, preferences.GetString(Constants.ExportPresets, ""));
    }
}
