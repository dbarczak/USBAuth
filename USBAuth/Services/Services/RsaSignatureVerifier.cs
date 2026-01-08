using Model;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{

    public class RsaSignatureVerifier : IRsaSignatureVerifier
    {
        public bool Verify(Device device, byte[] challenge, byte[] signature)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(device.PublicKeyPem.ToCharArray());

                return rsa.VerifyData(
                    challenge,
                    signature,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );
            }
            catch
            {
                return false;
            }
        }
    }
}
