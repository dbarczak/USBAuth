using Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class InMemoryChallengeStore : IChallengeStore
    {
        private readonly ConcurrentDictionary<string, byte[]> _map = new();

        public byte[] Create(string deviceId, int sizeBytes = 32)
        {
            byte[] ch = RandomNumberGenerator.GetBytes(sizeBytes);
            _map[deviceId] = ch;
            return ch;
        }

        public bool TryConsume(string deviceId, byte[] challenge)
        {
            if (!_map.TryGetValue(deviceId, out var stored))
                return false;

            bool ok = CryptographicOperations.FixedTimeEquals(stored, challenge);
            if (ok)
                _map.TryRemove(deviceId, out _);

            return ok;
        }
    }
}
