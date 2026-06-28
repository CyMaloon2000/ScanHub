using System;
using System.Security.Cryptography;
using System.Text;

namespace InventoryScanner.Helpers
{
    /// <summary>
    /// Provides cryptographic helper methods for password hashing and verification.
    /// </summary>
    public static class SecurityHelper
    {
        private const int SaltSize = 32;
        private const int HashIterations = 10000;
        private const int HashSize = 32;

        /// <summary>
        /// Generates a cryptographically secure random salt.
        /// </summary>
        public static string GenerateSalt()
        {
            byte[] salt = new byte[SaltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Hashes a password using PBKDF2 with the given salt.
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                saltBytes,
                HashIterations,
                HashAlgorithmName.SHA256,
                HashSize);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verifies a password against a stored hash and salt.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            string hash = HashPassword(password, storedSalt);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(hash),
                Convert.FromBase64String(storedHash));
        }

        /// <summary>
        /// Generates a secure session token.
        /// </summary>
        public static string GenerateSessionToken()
        {
            byte[] tokenBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes);
        }
    }
}
