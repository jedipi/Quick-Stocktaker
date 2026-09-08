namespace QuickStockTaker.Core.Services.Interfaces;

public interface IExportPresetService
{
    Task<IReadOnlyList<ExportPreset>> GetPresetsAsync();
    Task SaveAsync(ExportPreset preset);
    Task DeleteAsync(string id);
    Task SelectAsync(string id);
    Task<ExportPresetSelection> GetSelectionAsync();
    Task<string> PreviewAsync(ExportPreset preset);
}
