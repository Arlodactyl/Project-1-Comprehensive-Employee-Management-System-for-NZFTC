using BCrypt.Net;

namespace NZFTC_EmployeeSystem
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}