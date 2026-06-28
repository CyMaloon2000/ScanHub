namespace InventoryScanner.Models
{
    /// <summary>
    /// Represents the possible application states.
    /// </summary>
    public enum AppState
    {
        Offline,
        Authenticated,
        Unauthenticated,
        Scanning,
        Loading,
        Importing,
        Exporting,
        Error
    }
}
