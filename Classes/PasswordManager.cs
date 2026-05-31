using System;

namespace PasswordCracker.Classes
{
    public class PasswordManager
    {
        private const string CHARSET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public string PlainPassword { get; private set; }
        public string HashedPassword { get; private set; }
        public int PasswordLength { get; private set; }

        private readonly Random _random = new Random();

        public void GenerateNewPassword()
        {
            // Random length: 4 or 5 (between 4 inclusive and 6 exclusive)
            PasswordLength = _random.Next(4, 6);

            char[] passwordChars = new char[PasswordLength];
            for (int i = 0; i < PasswordLength; i++)
                passwordChars[i] = CHARSET[_random.Next(CHARSET.Length)];

            PlainPassword = new string(passwordChars);
            HashedPassword = HashHelper.Hash(PlainPassword);
        }

        public bool SetCustomPassword(string password)
        {
            if (password == null || password.Length < 4 || password.Length > 5)
                return false;

            foreach (char c in password)
                if (!CHARSET.Contains(c.ToString()))
                    return false;

            PlainPassword = password;
            HashedPassword = HashHelper.Hash(password);
            PasswordLength = password.Length;
            return true;
        }

        public static string GetCharset() => CHARSET;
    }
}