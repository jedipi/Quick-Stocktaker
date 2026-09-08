namespace QuickStockTaker.Core.Services;

public sealed class ExportPreset
{
    public static readonly string[] Fields =
    [
        "DeviceId", "StocktakeNumber", "Site", "BayLocation", "Barcode", "Description",
        "Qty", "StocktakeDate", "InsertedAt", "UpdatedAt", "Id"
    ];

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New preset";
    public string Delimiter { get; set; } = ",";
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public bool IncludeHeader { get; set; } = true;
    public List<ExportPresetColumn> Columns { get; set; } = Fields
        .Select(field => new ExportPresetColumn { Field = field, Header = field }).ToList();

    public static ExportPreset CreateDefault() => new();
}

public sealed class ExportPresetColumn
{
    public string Field { get; set; }
    public string Header { get; set; }
    public bool Included { get; set; } = true;
}

public sealed record ExportPresetSelection(ExportPreset Preset, string Notice);

public sealed class ExportPresetException(string message) : InvalidOperationException(message);
