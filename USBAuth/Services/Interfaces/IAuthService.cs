using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IAuthService
    {
        Task<Device> AuthenticatePin(string deviceId, string pin);
        Task<byte[]> GenerateChallenge(string deviceId);
        Task<bool> ValidateToken(string token);
    }
}
