using System;
using System.Threading.Tasks;
using InventoryScanner.Helpers;
using InventoryScanner.Models;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    /// <summary>
    /// Handles offline authentication using locally stored credentials.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEventAggregator _eventAggregator;

        public AuthenticationService(IUserRepository userRepository, IEventAggregator eventAggregator)
        {
            _userRepository = userRepository;
            _eventAggregator = eventAggregator;
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            try
            {
                var (isValid, errorMsg) = ValidationHelper.ValidateLogin(username, password);
                if (!isValid)
                    return (false, errorMsg);

                var user = await _userRepository.GetByUsernameAsync(username.Trim());
                if (user == null)
                    return (false, "Invalid username or password.");

                if (!SecurityHelper.VerifyPassword(password, user.PasswordHash, user.Salt))
                    return (false, "Invalid username or password.");

                // Store session in SecureStorage
                string token = SecurityHelper.GenerateSessionToken();
                await SecureStorage.Default.SetAsync(Constants.SessionTokenKey, token);
                await SecureStorage.Default.SetAsync(Constants.CurrentUserKey, user.Username);
                await SecureStorage.Default.SetAsync(Constants.IsAuthenticatedKey, "true");

                _eventAggregator.Publish(new AuthStateChangedEvent
                {
                    IsAuthenticated = true,
                    Username = user.Username
                });

                return (true, $"Welcome, {user.DisplayName}!");
            }
            catch (Exception ex)
            {
                return (false, $"Login failed: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task LogoutAsync()
        {
            try
            {
                SecureStorage.Default.Remove(Constants.SessionTokenKey);
                SecureStorage.Default.Remove(Constants.CurrentUserKey);
                SecureStorage.Default.Remove(Constants.IsAuthenticatedKey);

                _eventAggregator.Publish(new AuthStateChangedEvent
                {
                    IsAuthenticated = false,
                    Username = string.Empty
                });
            }
            catch (Exception)
            {
                // SecureStorage may throw on some devices, ensure logout completes
                SecureStorage.Default.RemoveAll();
            }

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var value = await SecureStorage.Default.GetAsync(Constants.IsAuthenticatedKey);
                return value == "true";
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetCurrentUsernameAsync()
        {
            try
            {
                return await SecureStorage.Default.GetAsync(Constants.CurrentUserKey) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
