namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IPageDialogService
    {
        Task<string> DisplayPromptAsync(
            string title,
            string message,
            string accept = null,
            string cancel = null,
            string placeholder = null,
            Keyboard keyboard = null);

        Task DisplayAlertAsync(string title, string message, string cancel);

        Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel);

        Task<string> DisplayActionSheetAsync(string title, string cancel, string destruction, params string[] buttons);
    }
}
