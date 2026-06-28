using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    public class GoogleMlKitScannerService : IScannerImplementationService
    {
        public string ModeName => "Scan (V2)";

        public Task StartAsync(int sessionId)
        {
            return Shell.Current.GoToAsync("ScannerV2", new Dictionary<string, object>
            {
                { "SessionId", sessionId }
            });
        }
    }
}
