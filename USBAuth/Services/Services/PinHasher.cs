using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class PinHasher : IPinHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public void HashPin(string pin, out byte[] salt, out byte[] hash)
        {
            salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var kdf = new Rfc2898DeriveBytes(pin, salt, Iterations, HashAlgorithmName.SHA256);
            hash = kdf.GetBytes(KeySize);
        }

        public bool VerifyPin(string pin, byte[] salt, byte[] expectedHash)
        {
            using var kdf = new Rfc2898DeriveBytes(pin, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] computed = kdf.GetBytes(expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(computed, expectedHash);
        }
    }
}
