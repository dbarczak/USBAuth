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
    public class TokenValidator : ITokenValidator
    {
        private readonly AppDbContext _db;

        public TokenValidator(AppDbContext db) => _db = db;

        public async Task<Session?> ValidateAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var session = await _db.Sessions
                .Include(s => s.Device)
                .FirstOrDefaultAsync(s => s.Token == token);

            if (session == null) return null;
            if (session.IsRevoked) return null;
            if (session.ExpiresAt <= DateTime.UtcNow) return null;

            if (session.Device == null) return null;
            if (!string.Equals(session.Device.Status, "Active", StringComparison.OrdinalIgnoreCase))
                return null;

            return session;
        }
    }
}
