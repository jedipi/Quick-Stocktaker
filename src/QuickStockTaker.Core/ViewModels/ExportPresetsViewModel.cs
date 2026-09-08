using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.ViewModels;

public partial class ExportPresetsViewModel : ObservableObject
{
    private readonly IExportPresetService _presets;
    private readonly IEntitlementService _entitlement;
    private string _draftId;
    private bool _loading;
    private static readonly string[] DelimiterValues = [",", ";", "\t", "|"];
    private static readonly string[] DateValues = ["yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy"];

    public string[] Delimiters { get; } = ["Comma (,)", "Semicolon (;)", "Tab", "Pipe (|)"];
    public string[] DateFormats { get; } = ["yyyy-MM-dd (2026-09-07)", "dd/MM/yyyy (07/09/2026)", "MM/dd/yyyy (09/07/2026)"];
    public ObservableCollection<ExportColumnViewModel> Columns { get; } = [];
    [ObservableProperty] private IReadOnlyList<ExportPreset> _savedPresets = [];
    [ObservableProperty] private ExportPreset _selectedPreset;
    [ObservableProperty] private string _presetName;
    [ObservableProperty] private int _delimiterIndex;
    [ObservableProperty] private int _dateFormatIndex;
    [ObservableProperty] private bool _includeHeader;
    [ObservableProperty] private string _preview;
    [ObservableProperty] private string _message;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanEdit))] private bool _isPro;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanEdit)), NotifyPropertyChangedFor(nameof(CanSelect))] private bool _isBusy;
    public bool CanEdit => IsPro && !IsBusy;
    public bool CanSelect => !IsBusy;

    public ExportPresetsViewModel(IExportPresetService presets, IEntitlementService entitlement)
    {
        _presets = presets;
        _entitlement = entitlement;
    }

    public Task LoadAsync() => RunAsync(async () =>
    {
        await _entitlement.InitializeAsync();
        IsPro = _entitlement.IsPro;
        await ReloadAsync(SelectedPreset?.Id);
        if (!IsPro)
            Message = "Custom export presets require Pro. Your saved presets are retained. Upgrade from About.";
    });

    partial void OnSelectedPresetChanged(ExportPreset value)
    {
        if (!_loading)
            LoadDraft(value ?? ExportPreset.CreateDefault());
    }

    partial void OnPresetNameChanged(string value) => Preview = "";
    partial void OnDelimiterIndexChanged(int value) => Preview = "";
    partial void OnDateFormatIndexChanged(int value) => Preview = "";
    partial void OnIncludeHeaderChanged(bool value) => Preview = "";

    [RelayCommand]
    private void NewPreset()
    {
        if (!CanEdit)
            return;
        SelectedPreset = null;
        LoadDraft(ExportPreset.CreateDefault());
        Message = "New preset: choose a unique name, then save.";
    }

    [RelayCommand]
    private Task Save() => RunAsync(async () =>
    {
        var preset = BuildDraft();
        await _presets.SaveAsync(preset);
        await ReloadAsync(preset.Id);
        Message = "Preset saved. Select it on the delivery page to use it.";
    });

    [RelayCommand]
    private Task Delete() => RunAsync(async () =>
    {
        if (SelectedPreset == null)
        {
            Message = "Select a saved preset to delete.";
            return;
        }
        await _presets.DeleteAsync(SelectedPreset.Id);
        await ReloadAsync(null);
        Message = "Preset deleted. If it was selected for delivery, Default CSV will be used.";
    });

    [RelayCommand]
    private Task ShowPreview() => RunAsync(async () => Preview = await _presets.PreviewAsync(BuildDraft()));

    [RelayCommand]
    private void MoveUp(ExportColumnViewModel column) => Move(column, -1);

    [RelayCommand]
    private void MoveDown(ExportColumnViewModel column) => Move(column, 1);

    private void Move(ExportColumnViewModel column, int offset)
    {
        if (!CanEdit)
            return;
        var index = Columns.IndexOf(column);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= Columns.Count)
            return;
        Columns.Move(index, target);
        Preview = "";
    }

    private async Task ReloadAsync(string id)
    {
        _loading = true;
        try
        {
            SavedPresets = await _presets.GetPresetsAsync();
            SelectedPreset = SavedPresets.FirstOrDefault(preset => preset.Id == id);
            LoadDraft(SelectedPreset ?? ExportPreset.CreateDefault());
        }
        finally
        {
            _loading = false;
        }
    }

    private void LoadDraft(ExportPreset preset)
    {
        _draftId = preset.Id;
        PresetName = preset.Name;
        DelimiterIndex = Array.IndexOf(DelimiterValues, preset.Delimiter);
        DateFormatIndex = Array.IndexOf(DateValues, preset.DateFormat);
        IncludeHeader = preset.IncludeHeader;
        Columns.Clear();
        foreach (var column in preset.Columns)
        {
            var draft = new ExportColumnViewModel(column);
            draft.PropertyChanged += (_, _) => Preview = "";
            Columns.Add(draft);
        }
        Preview = "";
    }

    private ExportPreset BuildDraft() => new()
    {
        Id = _draftId,
        Name = PresetName,
        Delimiter = DelimiterIndex >= 0 && DelimiterIndex < DelimiterValues.Length ? DelimiterValues[DelimiterIndex] : "",
        DateFormat = DateFormatIndex >= 0 && DateFormatIndex < DateValues.Length ? DateValues[DateFormatIndex] : "",
        IncludeHeader = IncludeHeader,
        Columns = Columns.Select(column => new ExportPresetColumn
        {
            Field = column.Field,
            Header = column.Header,
            Included = column.Included
        }).ToList()
    };

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        Message = "";
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Preview = "";
            Message = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public partial class ExportColumnViewModel : ObservableObject
{
    public string Field { get; }
    [ObservableProperty] private string _header;
    [ObservableProperty] private bool _included;

    public ExportColumnViewModel(ExportPresetColumn column)
    {
        Field = column.Field;
        Header = column.Header;
        Included = column.Included;
    }
}
