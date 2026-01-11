using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBAuthFront
{
    public class PinCheckResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int FailedAttempts { get; set; }
        public int MaxAttempts { get; set; }
        public string Status { get; set; } = "";
    }
}
