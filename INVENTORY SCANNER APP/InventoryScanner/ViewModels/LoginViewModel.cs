using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.ViewModels
{
    /// <summary>
    /// ViewModel for the login page. Handles offline authentication.
    /// </summary>
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;

        public LoginViewModel(IAuthenticationService authService)
        {
            _authService = authService;
            Title = "Login";
        }

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isPasswordVisible;

        /// <summary>
        /// Attempts to log in with the provided credentials.
        /// </summary>
        [RelayCommand]
        private async Task LoginAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var (success, message) = await _authService.LoginAsync(Username, Password);

                if (success)
                {
                    // Clear sensitive data
                    Password = string.Empty;
                    StatusMessage = message;

                    // Navigate to session list
                    await Shell.Current.GoToAsync("//Dashboard");
                }
                else
                {
                    HasError = true;
                    ErrorMessage = message;
                }
            }, "Login");
        }

        /// <summary>
        /// Toggles password visibility.
        /// </summary>
        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        /// <summary>
        /// Checks if user is already authenticated on page load.
        /// </summary>
        [RelayCommand]
        private async Task CheckAuthAsync()
        {
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            if (isAuthenticated)
            {
                await Shell.Current.GoToAsync("//Dashboard");
            }
        }
    }
}
