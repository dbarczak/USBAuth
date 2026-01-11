using DAL;
using Model;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class DeviceLockoutService : IDeviceLockoutService
    {
        private readonly AppDbContext _db;

        private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
        private const int MaxAttempts = 5;

        public DeviceLockoutService(AppDbContext db)
        {
            _db = db;
        }

        public async Task RegisterFailureAsync(Device device)
        {
            var now = DateTime.UtcNow;

            if (!device.FailedLoginWindowStartUtc.HasValue ||
                now - device.FailedLoginWindowStartUtc.Value > Window)
            {
                device.FailedLoginWindowStartUtc = now;
                device.FailedLoginCount = 0;
            }

            device.FailedLoginCount++;

            if (device.FailedLoginCount >= MaxAttempts)
            {
                device.Status = "Blocked";
            }

            await _db.SaveChangesAsync();
        }

        public async Task ResetFailuresAsync(Device device)
        {
            device.FailedLoginCount = 0;
            device.FailedLoginWindowStartUtc = null;
            await _db.SaveChangesAsync();
        }
    }
}
