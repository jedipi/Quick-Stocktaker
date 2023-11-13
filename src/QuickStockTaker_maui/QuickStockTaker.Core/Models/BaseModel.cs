using SQLite;

namespace B2CUtilities.CoreMaui.SQLiteModel
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
