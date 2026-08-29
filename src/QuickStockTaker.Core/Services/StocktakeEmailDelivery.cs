namespace QuickStockTaker.Core.Services
{
    internal sealed record StocktakeEmailDelivery(
        StocktakeExport Export,
        string Recipient,
        string Sender,
        StocktakeEmailConfiguration Configuration,
        StocktakeEmailContent Content);

    internal sealed record StocktakeEmailConfiguration(
        string Provider,
        string Host,
        int Port,
        string Username,
        string Password);

    internal sealed record StocktakeEmailContent(
        string DeviceId,
        int StocktakeNumber,
        string Site,
        string StocktakeDate);

    internal sealed record StocktakeEmailMessage(
        string Recipient,
        string Sender,
        string Subject,
        string HtmlBody,
        FileInfo Attachment);

    internal sealed record StocktakeEmailConfigurationInput(
        string Provider,
        string Sender,
        string Host,
        string Port,
        string Username,
        string Password);
}
