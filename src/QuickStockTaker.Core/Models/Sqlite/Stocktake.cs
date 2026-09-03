using SQLite;

namespace QuickStockTaker.Core.Models.Sqlite;

public sealed class Stocktake : BaseModel
{
    [NotNull]
    public string StocktakeNumber { get; set; }

    [NotNull]
    public string Site { get; set; }

    public DateTime StocktakeDate { get; set; }

    public DateTime UpdatedAt { get; set; }
}
