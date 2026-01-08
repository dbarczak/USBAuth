using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBAuthFront
{
    public class ChallengeResponseDto
    {
        public string DeviceId { get; set; } = default!;
        public string ChallengeBase64 { get; set; } = default!;
    }
}
