using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace InventoryScanner.Helpers
{
    /// <summary>
    /// Provides input validation helper methods.
    /// </summary>
    public static partial class ValidationHelper
    {
        /// <summary>
        /// Validates that a string is not null, empty, or whitespace.
        /// </summary>
        public static bool IsNotEmpty(string? value) =>
            !string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// Validates that a barcode/QR value is acceptable.
        /// </summary>
        public static bool IsValidBarcode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (value.Length < 1 || value.Length > 500) return false;
            // Allow alphanumeric, common barcode characters
            return BarcodeRegex().IsMatch(value);
        }

        /// <summary>
        /// Validates session required fields.
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateSession(
            string? school, string? campus, string? location, string? room)
        {
            if (!IsNotEmpty(school))
                return (false, "School is required.");
            if (!IsNotEmpty(campus))
                return (false, "Campus is required.");
            if (!IsNotEmpty(location))
                return (false, "Location is required.");
            if (!IsNotEmpty(room))
                return (false, "Room is required.");
            return (true, string.Empty);
        }

        /// <summary>
        /// Validates login credentials format.
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateLogin(
            string? username, string? password)
        {
            if (!IsNotEmpty(username))
                return (false, "Username is required.");
            if (!IsNotEmpty(password))
                return (false, "Password is required.");
            if (password!.Length < 4)
                return (false, "Password must be at least 4 characters.");
            return (true, string.Empty);
        }

        [GeneratedRegex(@"^[a-zA-Z0-9\-\._:;/\\+=#\$%&\*\?!@\(\)\[\]\{\}\|~`,<>^' ]+$")]
        private static partial Regex BarcodeRegex();
    }
}
