using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IChallengeStore
    {
        byte[] Create(string deviceId, int sizeBytes = 32);
        bool TryConsume(string deviceId, byte[] challenge);
    }
}
