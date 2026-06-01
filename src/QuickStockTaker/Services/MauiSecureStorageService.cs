using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Services;

public class MauiSecureStorageService : ISecureStorageService
{
    public Task<string> GetAsync(string key) => SecureStorage.GetAsync(key);

    public Task SetAsync(string key, string value) => SecureStorage.SetAsync(key, value);
}
