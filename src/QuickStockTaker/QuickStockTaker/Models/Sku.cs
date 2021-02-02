using System;
using System.Collections.Generic;
using System.Text;
using PropertyChanged;
using SQLite;

namespace QuickStockTaker.Models
{
    [AddINotifyPropertyChangedInterface]
    public partial class Sku
    {
        [PrimaryKey, AutoIncrement]
        public Int64 Id { get; set; }

        [NotNull]
        public Int64 Barcode { get; set; }

        [NotNull]
        public String Description { get; set; }

    }
}
