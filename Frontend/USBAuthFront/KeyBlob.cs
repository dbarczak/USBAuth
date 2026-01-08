using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBAuthFront
{
    public class KeyBlob
    {
        public int Version { get; set; } = 1;
        public string DeviceId { get; set; } = default!;
        public string Salt2Base64 { get; set; } = default!;
        public string NonceBase64 { get; set; } = default!;
        public string CiphertextBase64 { get; set; } = default!;
        public string TagBase64 { get; set; } = default!;
    }
}
