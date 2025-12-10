using DTOs;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IDeviceService
    {
        Task<Device> RegisterDevice(RegisterDeviceRequest request);
        Task<bool> BlockDevice(int id);
        Task<bool> UnblockDevice(int id);
    }
}
