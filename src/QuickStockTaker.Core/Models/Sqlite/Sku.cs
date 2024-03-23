using SQLite;

namespace QuickStockTaker.Core.Models.Sqlite
{
    public class Sku : BaseModel
    {
        [NotNull]
        public long Barcode { get; set; }

        [NotNull]
        public string Description { get; set; }

    }
}
