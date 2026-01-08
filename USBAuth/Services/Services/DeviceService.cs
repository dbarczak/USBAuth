using DAL;
using DTOs;
using Microsoft.EntityFrameworkCore;
using Model;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly AppDbContext _db;
        private readonly IPinHasher _pinHasher;

        public DeviceService(AppDbContext db, IPinHasher pinHasher)
        {
            _db = db;
            _pinHasher = pinHasher;
        }

        public Task<bool> DeviceExistsAsync(string deviceId)
            => _db.Devices.AnyAsync(d => d.DeviceId == deviceId);

        public async Task<Device> RegisterDeviceAsync(RegisterDeviceRequest request)
        {
            _pinHasher.HashPin(request.Pin, out var salt, out var hash);

            var device = new Device
            {
                DeviceId = request.DeviceId,
                OwnerName = request.OwnerName,
                PublicKeyPem = request.PublicKeyPem,
                PinSalt = salt,
                PinHash = hash,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _db.Devices.Add(device);
            await _db.SaveChangesAsync();
            return device;
        }

        public async Task<Device?> GetByDeviceIdAsync(string deviceId)
        {
            return await _db.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId);
        }
    }
}
