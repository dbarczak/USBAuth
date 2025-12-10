using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICryptoService
    {
        void HashPin(string pin, out byte[] salt, out byte[] hash);
        bool VerifyPin(string pin, byte[] salt, byte[] hash);
        bool VerifySignature(byte[] challenge, byte[] signature, string publicKeyPem);
    }
}
