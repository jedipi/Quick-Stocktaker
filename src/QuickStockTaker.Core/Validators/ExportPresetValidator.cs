using FluentValidation;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.Core.Validators;

public sealed class ExportPresetValidator : AbstractValidator<ExportPreset>
{
    public ExportPresetValidator()
    {
        RuleFor(preset => preset.Id).NotEmpty();
        RuleFor(preset => preset.Name).NotEmpty().MaximumLength(100)
            .Must(name => !string.Equals(name?.Trim(), "Default CSV", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Choose a name other than Default CSV.");
        RuleFor(preset => preset.Delimiter).Must(value => value is "," or ";" or "\t" or "|")
            .WithMessage("Choose comma, semicolon, tab, or pipe.");
        RuleFor(preset => preset.DateFormat).Must(value => value is "yyyy-MM-dd" or "dd/MM/yyyy" or "MM/dd/yyyy")
            .WithMessage("Choose one of the supported date formats.");
        RuleFor(preset => preset.Columns).Must(columns => columns is { Count: > 0 }
                && columns.All(column => column != null && ExportPreset.Fields.Contains(column.Field))
                && columns.Select(column => column.Field).Distinct().Count() == columns.Count
                && columns.Any(column => column.Included))
            .WithMessage("Include at least one column and use each available field at most once.");
        RuleFor(preset => preset).Must(preset => !preset.IncludeHeader || preset.Columns == null
                || preset.Columns.Where(column => column is { Included: true })
                    .All(column => !string.IsNullOrWhiteSpace(column.Header)))
            .WithMessage("Enter a header for each included column.");
    }
}
