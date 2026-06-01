using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Services;

public class MauiAppPreferences : IAppPreferences
{
    public string GetString(string key, string defaultValue) => Preferences.Get(key, defaultValue);

    public bool GetBool(string key, bool defaultValue) => Preferences.Get(key, defaultValue);

    public DateTime GetDateTime(string key, DateTime defaultValue) => Preferences.Get(key, defaultValue);

    public int GetInt(string key, int defaultValue) => Preferences.Get(key, defaultValue);

    public void Set(string key, string value) => Preferences.Set(key, value);

    public void Set(string key, bool value) => Preferences.Set(key, value);

    public void Set(string key, DateTime value) => Preferences.Set(key, value);

    public void Set(string key, int value) => Preferences.Set(key, value);
}
