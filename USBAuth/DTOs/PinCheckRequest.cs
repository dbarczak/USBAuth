using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class PinCheckRequest
    {
        public string DeviceId { get; set; } = default!;
        public string Pin { get; set; } = default!;
    }
}
