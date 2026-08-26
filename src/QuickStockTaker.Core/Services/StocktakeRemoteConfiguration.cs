namespace QuickStockTaker.Core.Services
{
    internal enum StocktakeRemoteProtocol
    {
        Ftp,
        Sftp
    }

    internal sealed record StocktakeRemoteConfiguration(
        StocktakeRemoteProtocol Protocol,
        string Host,
        int Port,
        string Folder,
        string Username,
        string Password);

    internal sealed record StocktakeRemoteConfigurationInput(
        bool UseSftp,
        string Host,
        string Port,
        string Folder,
        string Username,
        string Password);
}
