using System.Threading.Tasks;
using SQLite;

namespace InventoryScanner.Services.Interfaces
{
    /// <summary>
    /// Interface for database initialization and connection management.
    /// </summary>
    public interface IDatabaseService
    {
        string DatabasePath { get; }
        Task<SQLiteAsyncConnection> GetConnectionAsync();
        Task CloseAsync();
    }
}
