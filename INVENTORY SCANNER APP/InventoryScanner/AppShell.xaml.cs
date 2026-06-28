using System.Windows.Input;
using InventoryScanner.Services.Interfaces;
using InventoryScanner.Views;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryScanner
{
    public partial class AppShell : Shell
    {
        private readonly ISetupService _setupService;
        private readonly IAuthenticationService _authenticationService;

        public AppShell(ISetupService setupService, IAuthenticationService authenticationService)
        {
            _setupService = setupService;
            _authenticationService = authenticationService;
            InitializeComponent();

            // Register routes for pages that are navigated to dynamically
            Routing.RegisterRoute("SessionDetail", typeof(SessionDetailPage));
            Routing.RegisterRoute("ScanHistory", typeof(ScanHistoryPage));
            Routing.RegisterRoute("Scanner", typeof(ScannerPage));
            Routing.RegisterRoute("ScannerV2", typeof(ScannerV2Page));

            BindingContext = this;

            Loaded += OnShellLoaded;
        }

        public ICommand LogoutCommand => new Command(async () => await LogoutAsync());

        private async void OnShellLoaded(object? sender, EventArgs e)
        {
            Loaded -= OnShellLoaded;

            try
            {
                if (!await _setupService.IsSetupCompleteAsync())
                {
                    await GoToAsync("//Setup");
                    return;
                }

                if (!await _authenticationService.IsAuthenticatedAsync())
                {
                    await GoToAsync("//Login");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shell startup routing failed: {ex}");
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                var page = Shell.Current?.CurrentPage;
                if (page == null) return;

                bool confirmed = await MainThread.InvokeOnMainThreadAsync(async () =>
                    await page.DisplayAlertAsync("Logout", "Are you sure you want to log out?", "Yes", "No"));

                if (!confirmed) return;

                var authService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IAuthenticationService>();
                if (authService != null)
                {
                    await authService.LogoutAsync();
                }

                await GoToAsync("//Login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogoutAsync failed: {ex}");
            }
        }
    }
}
