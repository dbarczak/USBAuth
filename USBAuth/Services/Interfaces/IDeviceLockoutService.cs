using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IDeviceLockoutService
    {
        Task RegisterFailureAsync(Device device);
        Task ResetFailuresAsync(Device device);
    }
}
