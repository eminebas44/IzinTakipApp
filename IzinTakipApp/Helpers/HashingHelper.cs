using System;
using System.Security.Cryptography;
using System.Text;

namespace IzinTakipApp.Helpers
{
    public static class HashingHelper
    {
        public static string CreatePasswordHash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static bool VerifyPasswordHash(string password, string storedHash)
        {
            var newHash = CreatePasswordHash(password);
            return newHash == storedHash;
        }
    }
}