using System.Security.Cryptography;
using System.Text;

namespace AuthApi.Helpers
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Creates a SHA-512 HMAC password hash and salt.
        /// </summary>
        /// <param name="password">The plaintext password to hash.</param>
        /// <param name="passwordHash">Output byte array of the computed hash.</param>
        /// <param name="passwordSalt">Output byte array of the randomly generated salt.</param>
        public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Value cannot be empty or whitespace.", nameof(password));

            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;                                    // random 128-byte key
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        /// <summary>
        /// Verifies a plaintext password against a stored hash and salt.
        /// </summary>
        /// <param name="password">The plaintext password to verify.</param>
        /// <param name="storedHash">The stored hash byte array.</param>
        /// <param name="storedSalt">The stored salt byte array.</param>
        /// <returns>True if the password matches; otherwise false.</returns>
        public static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Value cannot be empty or whitespace.", nameof(password));
            if (storedHash == null || storedHash.Length == 0)
                throw new ArgumentException("Invalid length of password hash (zero).", nameof(storedHash));
            if (storedSalt == null || storedSalt.Length == 0)
                throw new ArgumentException("Invalid length of password salt (zero).", nameof(storedSalt));

            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            // constant-time comparison to thwart timing attacks
            if (computedHash.Length != storedHash.Length) return false;
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != storedHash[i]) return false;
            }

            return true;
        }
    }
}
