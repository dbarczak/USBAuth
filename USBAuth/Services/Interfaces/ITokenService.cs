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
        string GenerateToken(Device device);
        Task<Session> CreateSession(Device device, string token);
        Task<Session> GetSessionByToken(string token);
    }
}
