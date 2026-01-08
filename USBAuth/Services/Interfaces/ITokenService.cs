using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken();
        Task<Session> CreateSessionAsync(Device device, string token, TimeSpan ttl);
        Task<bool> RevokeTokenAsync(string token);
    }
}
