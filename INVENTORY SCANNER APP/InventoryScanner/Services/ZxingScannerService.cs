using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    public class ZxingScannerService : IScannerImplementationService
    {
        public string ModeName => "Scan (V1)";

        public Task StartAsync(int sessionId)
        {
            return Shell.Current.GoToAsync("Scanner", new Dictionary<string, object>
            {
                { "SessionId", sessionId }
            });
        }
    }
}
