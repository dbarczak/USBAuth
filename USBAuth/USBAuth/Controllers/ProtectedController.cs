using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace USBAuth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProtectedController : ControllerBase
    {
        private readonly ITokenValidator _validator;

        public ProtectedController(ITokenValidator validator)
        {
            _validator = validator;
        }

        // GET /api/protected/ping
        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            string? auth = Request.Headers.Authorization;

            if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Brak nagłówka Authorization: Bearer <token>.");

            string token = auth.Substring("Bearer ".Length).Trim();
            var session = await _validator.ValidateAsync(token);

            if (session == null)
                return Unauthorized("Token nieprawidłowy / wygasł / wylogowany.");

            return Ok(new
            {
                message = "OK - masz dostęp.",
                deviceId = session.Device?.DeviceId,
                expiresAt = session.ExpiresAt
            });
        }
    }
}
