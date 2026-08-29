using FluentValidation;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.Core.Validators
{
    internal sealed class StocktakeRemoteConfigurationValidator : AbstractValidator<StocktakeRemoteConfigurationInput>
    {
        public StocktakeRemoteConfigurationValidator()
        {
            RuleFor(configuration => configuration.Host)
                .NotEmpty()
                .WithMessage("FTP/SFTP host is not configured or not valid.");
            RuleFor(configuration => configuration.Port)
                .Must(IsValidPort)
                .WithMessage("FTP/SFTP port is not configured or not valid.");
            RuleFor(configuration => configuration.Username)
                .NotEmpty()
                .WithMessage("FTP/SFTP username is not configured.");
            RuleFor(configuration => configuration.Password)
                .NotEmpty()
                .WithMessage("FTP/SFTP password is not configured.");
        }

        private static bool IsValidPort(string port)
        {
            return int.TryParse(port, out var parsedPort) && parsedPort is >= 1 and <= 65535;
        }
    }
}
