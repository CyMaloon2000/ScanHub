using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InventoryScanner.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels providing common functionality.
    /// </summary>
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        /// <summary>
        /// Inverse of IsBusy for binding.
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Safely resolves the current visible page for displaying alerts.
        /// Avoids JavaProxyThrowable by preferring Shell.Current.CurrentPage.
        /// </summary>
        private static Page? GetSafePage()
        {
            try
            {
                // Prefer the Shell's current content page (avoids flyout context crash)
                var shellPage = Shell.Current?.CurrentPage;
                if (shellPage != null) return shellPage;
            }
            catch { /* Shell may not be ready */ }

            try
            {
                if (Application.Current?.Windows.Count > 0)
                    return Application.Current.Windows[0].Page;
            }
            catch { /* Fallback also failed */ }

            return null;
        }

        /// <summary>
        /// Displays an alert dialog to the user.
        /// </summary>
        protected async Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            try
            {
                var page = GetSafePage();
                if (page == null) return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await page.DisplayAlertAsync(title, message, cancel));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowAlertAsync failed: {ex}");
            }
        }

        /// <summary>
        /// Displays a confirmation dialog and returns the result.
        /// </summary>
        protected async Task<bool> ShowConfirmAsync(string title, string message,
            string accept = "Yes", string cancel = "No")
        {
            try
            {
                var page = GetSafePage();
                if (page == null) return false;

                return await MainThread.InvokeOnMainThreadAsync(async () =>
                    await page.DisplayAlertAsync(title, message, accept, cancel));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowConfirmAsync failed: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Navigates to a page by route.
        /// </summary>
        protected async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if (parameters != null)
                await Shell.Current.GoToAsync(route, parameters);
            else
                await Shell.Current.GoToAsync(route);
        }

        /// <summary>
        /// Navigates back.
        /// </summary>
        protected async Task NavigateBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Wraps an async operation with busy state management and error handling.
        /// </summary>
        protected async Task ExecuteBusyAsync(Func<Task> operation, string? errorContext = null)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;
                await operation();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"{errorContext ?? "Operation"} failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error in {errorContext}: {ex}");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }
    }
}
