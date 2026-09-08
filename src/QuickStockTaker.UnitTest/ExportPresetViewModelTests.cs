using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public class ExportPresetViewModelTests
{
    [Fact]
    public async Task DraftEditsDoNotChangeSavedPresetUntilSaveAndInvalidatePreview()
    {
        var entitlement = Substitute.For<IEntitlementService>();
        entitlement.IsPro.Returns(true);
        var service = new ExportPresetService(new TestAppPreferences(), entitlement,
            Substitute.For<IStocktakeWorkspace>(), Substitute.For<IStocktakeOperationsService>(), new ExportPresetValidator());
        var saved = new ExportPreset { Name = "Original" };
        await service.SaveAsync(saved);
        var vm = new ExportPresetsViewModel(service, entitlement);
        await vm.LoadAsync();
        vm.SelectedPreset = Assert.Single(vm.SavedPresets);
        vm.PresetName = "Changed";
        var barcode = vm.Columns.Single(column => column.Field == "Barcode");
        barcode.Header = "Code";
        vm.MoveUpCommand.Execute(barcode);
        Assert.Equal("Original", Assert.Single(await service.GetPresetsAsync()).Name);
        await vm.ShowPreviewCommand.ExecuteAsync(null);
        Assert.Contains("Example data", vm.Preview);
        barcode.Included = false;
        Assert.Empty(vm.Preview);
        await vm.SaveCommand.ExecuteAsync(null);
        var restored = Assert.Single(await service.GetPresetsAsync());
        Assert.Equal("Changed", restored.Name);
        Assert.False(restored.Columns.Single(column => column.Field == "Barcode").Included);
        Assert.Equal("Barcode", restored.Columns[3].Field);
        entitlement.IsPro.Returns(false);
        await vm.LoadAsync();
        Assert.False(vm.CanEdit);
    }

    [Fact]
    public async Task FallbackNoticeSurvivesAppearingAndStopsFirstDelivery()
    {
        var presets = TestCsvExportServiceFactory.DefaultPresets();
        presets.GetSelectionAsync().Returns(new ExportPresetSelection(null!, "Default CSV is now selected."), new ExportPresetSelection(null!, null!));
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        workflow.CreateExportAsync(Arg.Any<CancellationToken>()).Returns(StocktakeDeliveryResult.NoStocktakeData());
        var pageDialogs = Substitute.For<IPageDialogService>();
        var vm = CreateDelivery(presets, workflow, pageDialogs);
        await vm.AppearingCommand.ExecuteAsync(null);
        await vm.CsvCommand.ExecuteAsync(null);
        await workflow.DidNotReceive().CreateExportAsync(Arg.Any<CancellationToken>());
        await pageDialogs.Received(1).DisplayAlertAsync("Export format changed", "Default CSV is now selected.", "OK");
        await vm.CsvCommand.ExecuteAsync(null);
        await workflow.Received(1).CreateExportAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeliveryWaitsForPresetSelectionAndCancelNamedPresetCanBeSelected()
    {
        var presets = TestCsvExportServiceFactory.DefaultPresets();
        var preset = new ExportPreset { Name = "Cancel" };
        presets.GetPresetsAsync().Returns(new[] { preset });
        var selection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        presets.SelectAsync(preset.Id).Returns(selection.Task);
        var pageDialogs = Substitute.For<IPageDialogService>();
        pageDialogs.DisplayActionSheetAsync("Export format", "Cancel", null, Arg.Any<string[]>()).Returns("Preset: Cancel");
        var workflow = Substitute.For<IStocktakeDeliveryWorkflow>();
        var vm = CreateDelivery(presets, workflow, pageDialogs);
        var selecting = vm.SelectPresetCommand.ExecuteAsync(null);
        Assert.True(vm.IsSelectingPreset);
        await vm.CsvCommand.ExecuteAsync(null);
        await workflow.DidNotReceive().CreateExportAsync(Arg.Any<CancellationToken>());
        selection.SetResult();
        await selecting;
        await presets.Received(1).SelectAsync(preset.Id);
        Assert.False(vm.IsSelectingPreset);
    }

    private static DataUploadViewModel CreateDelivery(IExportPresetService presets,
        IStocktakeDeliveryWorkflow workflow, IPageDialogService pageDialogs) => new(
            Substitute.For<IUserDialogs>(), workflow, new EmailValidator(), Substitute.For<IAppFileSystem>(),
            pageDialogs, Substitute.For<IEntitlementService>(), presets, Substitute.For<INavigationService>(),
            NullLogger<DataUploadViewModel>.Instance);
}
