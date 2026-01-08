using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class VerifySignatureRequest
    {
        public string DeviceId { get; set; } = default!;
        public string ChallengeBase64 { get; set; } = default!;
        public string SignatureBase64 { get; set; } = default!;
    }
}
