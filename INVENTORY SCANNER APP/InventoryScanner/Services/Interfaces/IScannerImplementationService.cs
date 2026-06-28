namespace InventoryScanner.Services.Interfaces
{
    public interface IScannerImplementationService
    {
        string ModeName { get; }
        Task StartAsync(int sessionId);
    }
}
