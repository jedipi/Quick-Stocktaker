using System.Text.Json;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.Core.Services;

public sealed class ExportPresetService(
    IAppPreferences preferences,
    IEntitlementService entitlement,
    IStocktakeWorkspace workspace,
    IStocktakeOperationsService operations,
    ExportPresetValidator validator) : IExportPresetService
{
    public Task<IReadOnlyList<ExportPreset>> GetPresetsAsync() => Task.FromResult<IReadOnlyList<ExportPreset>>(Load());

    public async Task SaveAsync(ExportPreset preset)
    {
        await RequireProAsync();
        Validate(preset);
        var presets = Load();
        preset.Name = preset.Name.Trim();
        if (presets.Any(existing => existing.Id != preset.Id
                && string.Equals(existing.Name, preset.Name, StringComparison.OrdinalIgnoreCase)))
            throw new ExportPresetException("A preset with that name already exists.");
        var index = presets.FindIndex(existing => existing.Id == preset.Id);
        if (index < 0)
            presets.Add(preset);
        else
            presets[index] = preset;
        preferences.Set(Constants.ExportPresets, JsonSerializer.Serialize(presets));
    }

    public async Task DeleteAsync(string id)
    {
        await RequireProAsync();
        var presets = Load();
        presets.RemoveAll(preset => preset.Id == id);
        preferences.Set(Constants.ExportPresets, JsonSerializer.Serialize(presets));
        // Keep the selected id until GetSelectionAsync can explain the fallback.
    }

    public async Task SelectAsync(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            await RequireProAsync();
            var preset = Load().SingleOrDefault(preset => preset.Id == id)
                ?? throw new ExportPresetException("That preset no longer exists. Choose Default CSV.");
            Validate(preset);
        }
        preferences.Set(Constants.SelectedExportPreset, id ?? "");
    }

    public async Task<ExportPresetSelection> GetSelectionAsync()
    {
        var id = preferences.GetString(Constants.SelectedExportPreset, "");
        if (string.IsNullOrEmpty(id))
            return new(null, null);
        await entitlement.InitializeAsync();
        if (!entitlement.IsPro)
        {
            preferences.Set(Constants.SelectedExportPreset, "");
            return new(null, "Custom presets require Pro. Default CSV is now selected; your presets have been retained.");
        }
        var preset = Load().SingleOrDefault(preset => preset.Id == id);
        if (preset != null)
        {
            Validate(preset);
            return new(preset, null);
        }
        preferences.Set(Constants.SelectedExportPreset, "");
        return new(null, "The last-used preset was deleted. Default CSV is now selected.");
    }

    public async Task<string> PreviewAsync(ExportPreset preset)
    {
        if (preset != null)
        {
            await RequireProAsync();
            Validate(preset);
        }
        var current = await workspace.GetCurrentAsync();
        var items = current == null ? [] : (await operations.GetItemsAsync(current.Id)).Take(5).ToArray();
        var example = items.Length == 0;
        if (example)
        {
            current = new Stocktake { StocktakeDate = new DateTime(2026, 9, 7) };
            items = [new StocktakeItem
            {
                Id = 1, DeviceId = "SCANNER-01", StocktakeNumber = "ST-100", Site = "Warehouse",
                BayLocation = "A-01", Barcode = "001234567890", Description = "Sample item", Qty = 3,
                StocktakeDate = "2026-09-07", InsertedAt = "2026-09-07 14:30:00", UpdatedAt = "2026-09-07 14:35:00"
            }];
        }
        using var writer = new StringWriter();
        await CsvExportService.WriteAsync(writer, items, current, preset, CancellationToken.None);
        return (example ? "Example data (no Stocktake Items)" : "Preview: first five Stocktake Items")
            + Environment.NewLine + writer;
    }

    private async Task RequireProAsync()
    {
        await entitlement.InitializeAsync();
        if (!entitlement.IsPro)
            throw new ExportPresetException("Custom export presets require Quick Stocktaker Pro. Upgrade from About.");
    }

    private void Validate(ExportPreset preset)
    {
        if (preset == null)
            throw new ExportPresetException("Choose a custom preset.");
        var result = validator.Validate(preset);
        if (!result.IsValid)
            throw new ExportPresetException(result.Errors[0].ErrorMessage);
    }

    private List<ExportPreset> Load()
    {
        try
        {
            var presets = JsonSerializer.Deserialize<List<ExportPreset>>(
                preferences.GetString(Constants.ExportPresets, "[]"));
            if (presets == null || presets.Any(preset => preset == null)
                || presets.Select(preset => preset.Id).Distinct().Count() != presets.Count)
                throw new JsonException();
            return presets;
        }
        catch (JsonException)
        {
            throw new ExportPresetException("Saved presets could not be read. They have been retained. Choose Default CSV to export.");
        }
    }
}
