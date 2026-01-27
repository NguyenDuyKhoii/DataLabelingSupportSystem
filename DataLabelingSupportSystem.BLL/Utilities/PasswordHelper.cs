using System.Security.Cryptography;
using System.Text;

namespace DataLabelingSupportSystem.BLL.Utilities
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000; // Chậm = an toàn

        public static string HashPassword(string password)
        {
            // Tạo salt ngẫu nhiên
            byte[] salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            // Hash với PBKDF2
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(HashSize);

            // Salt + Hash → 1 string
            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(HashSize);

            for (int i = 0; i < HashSize; i++)
                if (hashBytes[i + SaltSize] != hash[i])
                    return false;

            return true;
        }
    }
}
