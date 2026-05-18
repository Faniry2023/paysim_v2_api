using System.Security.Cryptography;

namespace API_PAYSIM.Helpers
{
    public class EncryptionPasswordHelper
    {
        private const int SaltSize = 32;
        private const int HashSize = 32;
        private const int DefaultIterations = 300_000;

        public static string HashPassword(string password, int iterations = DefaultIterations)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);

            byte[] hash = pbkdf2.GetBytes(HashSize);

            return $"{iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string stored)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrWhiteSpace(stored)) return false;

            var parts = stored.Split(':');
            if (parts.Length != 3) return false;

            if (!int.TryParse(parts[0], out int iterations)) return false;

            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] hash = Convert.FromBase64String(parts[2]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);

            byte[] computedHash = pbkdf2.GetBytes(hash.Length);

            return CryptographicOperations.FixedTimeEquals(computedHash, hash);
        }
    }
}
