using System.Security.Cryptography;
using System.Text;

namespace ECommerceMVC.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password, string randomKey)
        {
            var data = password + randomKey;
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        public static string GenerateRandomKey()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static bool VerifyPassword(string password, string randomKey, string hashedPassword)
        {
            var hashOfInput = HashPassword(password, randomKey);
            return hashOfInput == hashedPassword;
        }

        public static string GenerateUsername(string fullName, string email)
        {
            // Remove Vietnamese accents and special characters
            var username = RemoveVietnameseAccents(fullName.ToLower().Replace(" ", ""));
            
            // Take first part of email if username is too short
            if (username.Length < 3)
            {
                var emailPart = email.Split('@')[0];
                username = RemoveVietnameseAccents(emailPart.ToLower());
            }

            // Add random numbers
            var random = new Random();
            username = username + random.Next(1000, 9999);

            // Ensure username is not too long
            if (username.Length > 20)
            {
                username = username.Substring(0, 16) + random.Next(1000, 9999);
            }

            return username;
        }

        private static string RemoveVietnameseAccents(string text)
        {
            var withAccents = "בא?ד???????ג?????יט???ך?????םל???ףע?ץ?פ???????????תש?????????‎?????";
            var withoutAccents = "aaaaaaaaaaaaaaaaaeeeeeeeeeeeiiiiiooooooooooooooooouuuuuuuuuuuyyyyyd";
            
            for (int i = 0; i < withAccents.Length; i++)
            {
                text = text.Replace(withAccents[i], withoutAccents[i]);
            }
            
            // Remove any remaining special characters
            return new string(text.Where(c => char.IsLetterOrDigit(c)).ToArray());
        }
    }
}

