using SQLite;

namespace QuickStockTaker.Models
{
    public partial class StocktakeItem
    {
        [PrimaryKey, AutoIncrement]
        public Int64 Id { get; set; }

        [NotNull]
        public Int64 DeviceId { get; set; }

        [NotNull]
        public Int64 StocktakeNumber { get; set; }

        [NotNull]
        public String Site { get; set; } // site or warehouse

        [NotNull]
        public String BayLocation { get; set; } //this value is also location number

        [NotNull]
        public String Barcode { get; set; }

        [NotNull]
        public String Description { get; set; }

        [NotNull]
        public Int64 Qty { get; set; }

        [NotNull]
        public String StocktakeDate { get; set; }

        [NotNull]
        public String InsertedAt { get; set; }

        [NotNull]
        public String UpdatedAt { get; set; }

    }
}
