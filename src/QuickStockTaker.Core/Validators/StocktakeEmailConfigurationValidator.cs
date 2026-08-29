using FluentValidation;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.Core.Validators
{
    internal sealed class StocktakeEmailConfigurationValidator : AbstractValidator<StocktakeEmailConfigurationInput>
    {
        public StocktakeEmailConfigurationValidator()
        {
            RuleFor(configuration => configuration.Sender)
                .NotEmpty()
                .WithMessage("SMTP sender is not configured or not valid.")
                .EmailAddress()
                .WithMessage("SMTP sender is not configured or not valid.");
            RuleFor(configuration => configuration.Host)
                .NotEmpty()
                .WithMessage("SMTP host is not configured or not valid.");
            RuleFor(configuration => configuration.Port)
                .Must(IsValidPort)
                .WithMessage("SMTP port is not configured or not valid.");
            RuleFor(configuration => configuration.Username)
                .NotEmpty()
                .WithMessage("SMTP username is not configured.");
            RuleFor(configuration => configuration.Password)
                .NotEmpty()
                .WithMessage("SMTP password is not configured.");
        }

        private static bool IsValidPort(string value) =>
            int.TryParse(value, out var port) && port is >= 1 and <= 65535;
    }
}
