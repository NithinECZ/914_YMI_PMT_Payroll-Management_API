using System;
using System.Security.Cryptography;

namespace YMI_PMT_PayrollManagement_API.Helpers
{
    /// <summary>
    /// PBKDF2 based password hashing with legacy password support.
    /// Supports both new format (salt:hash) and old plain-text passwords.
    /// Old passwords are automatically upgraded to new format on successful login.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;       // 128 bit salt
        private const int HashSize = 32;       // 256 bit hash
        private const int Iterations = 100_000;

        /// <summary>
        /// Generates a random salt, hashes the password with it,
        /// and returns a single combined string: "salt:hash" (both Base64 encoded).
        /// This combined string is what gets saved into Pass_Wd column.
        /// </summary>
        public static string HashPassword(string password)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password: password,
                salt: saltBytes,
                iterations: Iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: HashSize
            );

            string saltBase64 = Convert.ToBase64String(saltBytes);
            string hashBase64 = Convert.ToBase64String(hashBytes);

            // Combine salt and hash with a colon separator
            return $"{saltBase64}:{hashBase64}";
        }

        /// <summary>
        /// Verifies an entered password against stored password.
        /// Supports both new format (salt:hash) and old plain-text passwords.
        /// 
        /// Returns:
        /// - VerificationResult.Success: Password matches new PBKDF2 format
        /// - VerificationResult.LegacyMatch: Password matches old plain-text format (needs upgrade)
        /// - VerificationResult.Failed: Password doesn't match
        /// </summary>
        public static VerificationResult VerifyPasswordWithMigration(string enteredPassword, string storedPassword)
        {
            if (string.IsNullOrEmpty(storedPassword))
                return VerificationResult.Failed;

            // 👈 NEW: Check if it's the new format (contains colon)
            if (storedPassword.Contains(':'))
            {
                // New format: salt:hash
                bool isValid = VerifyNewFormat(enteredPassword, storedPassword);
                return isValid ? VerificationResult.Success : VerificationResult.Failed;
            }
            else
            {
                // 👈 NEW: Legacy format - could be plain text or old hash
                // Try plain text comparison (old system stored passwords as-is)
                bool isLegacyMatch = enteredPassword == storedPassword;
                return isLegacyMatch ? VerificationResult.LegacyMatch : VerificationResult.Failed;
            }
        }

        /// <summary>
        /// Verifies password against new PBKDF2 format only (internal use).
        /// </summary>
        private static bool VerifyNewFormat(string enteredPassword, string storedCombined)
        {
            if (string.IsNullOrEmpty(storedCombined) || !storedCombined.Contains(':'))
                return false;

            try
            {
                var parts = storedCombined.Split(':', 2);
                string saltBase64 = parts[0];
                string storedHashBase64 = parts[1];

                byte[] saltBytes = Convert.FromBase64String(saltBase64);

                byte[] computedHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                    password: enteredPassword,
                    salt: saltBytes,
                    iterations: Iterations,
                    hashAlgorithm: HashAlgorithmName.SHA256,
                    outputLength: HashSize
                );

                string computedHashBase64 = Convert.ToBase64String(computedHashBytes);

                return computedHashBase64 == storedHashBase64;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Legacy verification for backward compatibility (old plain-text passwords).
        /// </summary>
        public static bool VerifyPassword(string enteredPassword, string storedPassword)
        {
            var result = VerifyPasswordWithMigration(enteredPassword, storedPassword);
            return result == VerificationResult.Success || result == VerificationResult.LegacyMatch;
        }
    }

    /// <summary>
    /// Password verification result.
    /// Success: New format password matched
    /// LegacyMatch: Old format password matched (needs upgrade on next login)
    /// Failed: Password doesn't match
    /// </summary>
    public enum VerificationResult
    {
        Success = 0,        // ✅ New format matched
        LegacyMatch = 1,    // ⚠️ Old format matched (needs rehash)
        Failed = 2          // ❌ No match
    }
}