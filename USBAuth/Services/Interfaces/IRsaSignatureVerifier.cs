using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IRsaSignatureVerifier
    {
        bool Verify(Device device, byte[] challenge, byte[] signature);
    }
}
