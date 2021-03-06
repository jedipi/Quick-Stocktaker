﻿using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using QuickStockTaker.ViewModels;

namespace QuickStockTaker.Validators
{
    public class StocktakeValidator : AbstractValidator<NewStocktakeViewModel>
    {
        public StocktakeValidator()
        {
            RuleFor(x => x.StocktakeNumber).GreaterThan(0).WithMessage("Stocktake number must be positive number.");
            RuleFor(x => x.Site).NotEmpty().WithMessage("Please specify a stocktake site");


        }
    }
}
