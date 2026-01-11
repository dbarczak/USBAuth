using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPinHasher
    {
        void HashPin(string pin, out byte[] salt, out byte[] hash);
        bool VerifyPin(string pin, byte[] salt, byte[] expectedHash);
    }
}
