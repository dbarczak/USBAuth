using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ITokenValidator
    {
        Task<Session?> ValidateAsync(string token);
    }
}
