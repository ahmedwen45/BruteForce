using System;
using System.Security.Cryptography;
using System.Text;

namespace BruteForceApp
{
    /// <summary>
    /// Handles password generation, hashing with SHA256 + static salt, and validation.
    /// </summary>
    public class PasswordManager
    {
        // Constant static salt defined in application
        public const string SALT = "BruteForceApp_StaticSalt_2024";

        private static readonly Random _rng = new Random();
        private const string CHARSET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// Generates a random password with length randomly chosen in [4, 6) i.e. 4 or 5 chars.
        /// </summary>
        public string GeneratePassword()
        {
            int length = _rng.Next(4, 6); // [4, 6) → 4 or 5
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(CHARSET[_rng.Next(CHARSET.Length)]);
            return sb.ToString();
        }

        /// <summary>
        /// Hashes a plain-text password using SHA256 with the static salt.
        /// </summary>
        public string HashPassword(string password)
        {
            string salted = SALT + password;
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(salted));
            return Convert.ToHexString(bytes); // uppercase hex string
        }

        /// <summary>
        /// Checks whether a candidate password matches the stored hash.
        /// </summary>
        public bool Validate(string candidate, string targetHash)
        {
            return HashPassword(candidate) == targetHash;
        }

        public string Charset => CHARSET;
    }
}
