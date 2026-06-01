using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

internal sealed class TestAppPreferences : IAppPreferences
{
    private readonly Dictionary<string, object> _values = new();

    public string GetString(string key, string defaultValue) =>
        _values.TryGetValue(key, out var value) ? (string)value : defaultValue;

    public bool GetBool(string key, bool defaultValue) =>
        _values.TryGetValue(key, out var value) ? (bool)value : defaultValue;

    public DateTime GetDateTime(string key, DateTime defaultValue) =>
        _values.TryGetValue(key, out var value) ? (DateTime)value : defaultValue;

    public int GetInt(string key, int defaultValue) =>
        _values.TryGetValue(key, out var value) ? (int)value : defaultValue;

    public void Set(string key, string value) => _values[key] = value;

    public void Set(string key, bool value) => _values[key] = value;

    public void Set(string key, DateTime value) => _values[key] = value;

    public void Set(string key, int value) => _values[key] = value;
}
