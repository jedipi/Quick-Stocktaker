
using SQLite;

namespace QuickStockTaker.Models
{
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
