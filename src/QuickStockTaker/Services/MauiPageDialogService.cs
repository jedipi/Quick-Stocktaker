using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Services;

public class MauiPageDialogService : IPageDialogService
{
    public async Task<string> DisplayPromptAsync(
        string title,
        string message,
        string accept = null,
        string cancel = null,
        string placeholder = null,
        Keyboard keyboard = null)
    {
        var page = Application.Current.Windows.FirstOrDefault()?.Page;
        return await page.DisplayPromptAsync(title, message, accept, cancel, placeholder, keyboard: keyboard);
    }

    public async Task DisplayAlertAsync(string title, string message, string cancel)
    {
        var page = Application.Current.Windows.FirstOrDefault()?.Page;
        await page.DisplayAlertAsync(title, message, cancel);
    }

    public async Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel)
    {
        var page = Application.Current.Windows.FirstOrDefault()?.Page;
        return await page.DisplayAlertAsync(title, message, accept, cancel);
    }

    public async Task<string> DisplayActionSheetAsync(string title, string cancel, string destruction, params string[] buttons)
    {
        var page = Application.Current.Windows.FirstOrDefault()?.Page;
        return await page.DisplayActionSheetAsync(title, cancel, destruction, buttons);
    }
}
