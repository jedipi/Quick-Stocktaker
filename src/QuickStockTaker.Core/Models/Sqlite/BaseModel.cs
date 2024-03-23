using SQLite;

namespace QuickStockTaker.Core.Models.Sqlite
{
    /// <summary>
    /// the base model for SQLite entities
    /// </summary>
    public class BaseModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
    }
}
