using System;
using System.Collections.Generic;
using System.Text;
using PropertyChanged;

namespace QuickStockTaker.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Bay
    {
        public string BayLocation { get; set; }
        public long TotalCount { get; set; }
    }
}
