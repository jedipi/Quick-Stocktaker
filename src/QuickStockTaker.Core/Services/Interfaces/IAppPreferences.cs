namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IAppPreferences
    {
        string GetString(string key, string defaultValue);

        bool GetBool(string key, bool defaultValue);

        DateTime GetDateTime(string key, DateTime defaultValue);

        int GetInt(string key, int defaultValue);

        void Set(string key, string value);

        void Set(string key, bool value);

        void Set(string key, DateTime value);

        void Set(string key, int value);
    }
}
