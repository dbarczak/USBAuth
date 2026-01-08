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
        Task<bool> DeviceExistsAsync(string deviceId);
        Task<Device> RegisterDeviceAsync(RegisterDeviceRequest request);
        Task<Device?> GetByDeviceIdAsync(string deviceId);
    }
}
