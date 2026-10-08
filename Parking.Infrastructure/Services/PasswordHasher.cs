using Parking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Services
{
    public sealed class PasswordHasher : IPasswordHasher
    {
        private const int CurrentIterations = 600_000;
        private const int MinimumAcceptedIterations = 100_000;
        private const int MaximumAcceptedIterations = 2_000_000; 
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int MinimumPasswordLength = 15;
        private const int MaximumPasswordLength = 128;

        public string Hash(string password)
        {
            ArgumentNullException.ThrowIfNull(password);
            ValidateNewPasswordLength(password);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var subkey = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                CurrentIterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return string.Join('.',
                "v2",
                CurrentIterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(subkey));
        }
        public bool Verify(string password, string hash)
        {
            if (password is null || string.IsNullOrEmpty(hash))
                return false;

            if (CountUnicodeCodePoints(password) > MaximumPasswordLength)
                return false;

            var parts = hash.Split('.', StringSplitOptions.None);
            string iterationsText;
            string saltText;
            string subkeyText;

            if (parts.Length == 4 && parts[0] == "v2")
            {
                iterationsText = parts[1];
                saltText = parts[2];
                subkeyText = parts[3];
            }
            else if (parts.Length == 3)
            {
                iterationsText = parts[0];
                saltText = parts[1];
                subkeyText = parts[2];
            }
            else
            {
                return false;
            }

            if (!int.TryParse(
                    iterationsText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var iterations) ||
                iterations < MinimumAcceptedIterations ||
                iterations > MaximumAcceptedIterations)
            {
                return false;
            }

            Span<byte> salt = stackalloc byte[SaltSize];
            if (!Convert.TryFromBase64String(saltText, salt, out var saltBytesWritten) || saltBytesWritten != SaltSize)
                return false;

            Span<byte> expectedSubkey = stackalloc byte[HashSize];
            if (!Convert.TryFromBase64String(subkeyText, expectedSubkey, out var expectedSubkeyBytesWritten) || expectedSubkeyBytesWritten != HashSize)
                return false;

            Span<byte> actualSubkey = stackalloc byte[HashSize];
            Rfc2898DeriveBytes.Pbkdf2(password, salt, actualSubkey, iterations, HashAlgorithmName.SHA256);

            return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        }
        private static void ValidateNewPasswordLength(string password)
        {
            var length = CountUnicodeCodePoints(password);
            if (length < MinimumPasswordLength || length > MaximumPasswordLength)
            {
                throw new ArgumentException(
                    $"Password must be between {MinimumPasswordLength} and {MaximumPasswordLength} characters long.",
                    nameof(password));
            }
        }
        private static int CountUnicodeCodePoints(string s)
        {
            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsHighSurrogate(s[i]))
                {
                    i++; 
                }
                count++;
            }
            return count;
        }
    }
}
