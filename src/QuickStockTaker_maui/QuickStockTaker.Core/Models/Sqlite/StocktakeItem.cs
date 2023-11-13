using SQLite;

namespace QuickStockTaker.Core.Models.Sqlite
{
    public class StocktakeItem : BaseModel
    {

        [NotNull]
        public long DeviceId { get; set; }

        [NotNull]
        public long StocktakeNumber { get; set; }

        [NotNull]
        public string Site { get; set; } // site or warehouse

        [NotNull]
        public string BayLocation { get; set; } //this value is also location number

        [NotNull]
        public string Barcode { get; set; }

        [NotNull]
        public string Description { get; set; }

        [NotNull]
        public long Qty { get; set; }

        [NotNull]
        public string StocktakeDate { get; set; }

        [NotNull]
        public string InsertedAt { get; set; }

        [NotNull]
        public string UpdatedAt { get; set; }

    }
}
