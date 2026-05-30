using System.Security.Cryptography;
using System.Text;

namespace PasswordCracker.Classes
{
    public static class HashHelper
    {
        private const string SALT = "VU_MIF_SALT_2024_#!@";

        public static string Hash(string password)
        {
            string saltedPassword = SALT + password;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static bool Verify(string candidate, string targetHash)
        {
            return Hash(candidate) == targetHash;
        }
    }
}