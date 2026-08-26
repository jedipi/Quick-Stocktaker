using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.Core.Services
{
    internal sealed record StocktakeRemoteConfigurationSnapshot(
        StocktakeRemoteConfiguration Configuration,
        string ErrorMessage)
    {
        public static async Task<StocktakeRemoteConfigurationSnapshot> CaptureAsync(
            IAppPreferences preferences,
            ISecureStorageService secureStorage,
            StocktakeRemoteConfigurationValidator validator)
        {
            var useSftp = preferences.GetBool(Constants.FtpUseSftp, true);
            var input = new StocktakeRemoteConfigurationInput(
                useSftp,
                preferences.GetString(Constants.FtpHost, string.Empty),
                preferences.GetString(Constants.FtpPort, useSftp ? "22" : "21"),
                preferences.GetString(Constants.FtpFolder, string.Empty),
                await secureStorage.GetAsync(Constants.FtpUsername) ?? string.Empty,
                await secureStorage.GetAsync(Constants.FtpPassword) ?? string.Empty);
            var validation = validator.Validate(input);
            if (!validation.IsValid)
                return new StocktakeRemoteConfigurationSnapshot(null, validation.Errors[0].ErrorMessage);

            return new StocktakeRemoteConfigurationSnapshot(
                new StocktakeRemoteConfiguration(
                    input.UseSftp ? StocktakeRemoteProtocol.Sftp : StocktakeRemoteProtocol.Ftp,
                    input.Host.Trim(),
                    int.Parse(input.Port),
                    input.Folder,
                    input.Username,
                    input.Password),
                null);
        }
    }
}
