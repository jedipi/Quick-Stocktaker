using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;

namespace QuickStockTaker.Validators
{
    /// <summary>
    /// Validate email address
    /// </summary>
    public class EmailValidator: AbstractValidator<string>
    {
        public EmailValidator()
        {
            RuleFor(x => x).NotEmpty().WithMessage("Email address is required")
                                        .EmailAddress().WithMessage("A valid email address is required.");
        }
    }
}
