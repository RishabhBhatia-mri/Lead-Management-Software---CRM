using BCrypt.Net;

namespace LeadManagement.Helpers {
    public class PasswordHelper {
        public static string HashPassword(string password) {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hashedPassword) {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        public static string GenerateTemporaryPassword(int length = 10) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var tempPassword = new char[length];

            for (int i = 0; i < length; i++) {
                tempPassword[i] = chars[random.Next(chars.Length)];
            }

            return new string(tempPassword);
        }
    }
}
