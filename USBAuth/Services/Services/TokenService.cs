using DAL;
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

    public class TokenService : ITokenService
    {
        private readonly AppDbContext _db;
        public TokenService(AppDbContext db) => _db = db;

        public string GenerateToken() => Guid.NewGuid().ToString("N");

        public async Task<Session> CreateSessionAsync(Device device, string token, TimeSpan ttl)
        {
            var now = DateTime.UtcNow;

            var session = new Session
            {
                Token = token,
                DeviceId = device.Id,
                CreatedAt = now,
                ExpiresAt = now.Add(ttl),
                IsRevoked = false
            };

            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();

            return session;
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Token == token);
            if (session == null)
                return false;

            if (session.IsRevoked)
                return true;

            session.IsRevoked = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
