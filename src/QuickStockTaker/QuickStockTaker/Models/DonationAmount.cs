using System;
using System.Collections.Generic;
using System.Text;
using PropertyChanged;

namespace QuickStockTaker.Models
{
    [AddINotifyPropertyChangedInterface]
    public class DonationAmount
    {
        public decimal Amount { get; set; }
        public string AmountString { get; set; }
    }
}
