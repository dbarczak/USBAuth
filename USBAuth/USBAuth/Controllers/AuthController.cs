using DTOs;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Services;

namespace USBAuth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IChallengeStore _challengeStore;
        private readonly IRsaSignatureVerifier _sigVerifier;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly IDeviceLockoutService _lockout;
        private readonly IPinHasher _pinHasher;

        public AuthController(IDeviceService deviceService, IConfiguration config, IChallengeStore challengeStore,
        IRsaSignatureVerifier sigVerifier,ITokenService tokenService, IDeviceLockoutService lockout, IPinHasher pinHasher)
        {
            _deviceService = deviceService;
            _config = config;
            _challengeStore = challengeStore;
            _sigVerifier = sigVerifier;
            _tokenService = tokenService;
            _lockout = lockout;
            _pinHasher = pinHasher;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            // 1) walidacja
            if (request == null ||
                string.IsNullOrWhiteSpace(request.DeviceId) ||
                string.IsNullOrWhiteSpace(request.OwnerName) ||
                string.IsNullOrWhiteSpace(request.PublicKeyPem) ||
                string.IsNullOrWhiteSpace(request.Pin) ||
                string.IsNullOrWhiteSpace(request.RegistrationSecret))
            {
                return BadRequest("Wymagane: DeviceId, OwnerName, PublicKeyPem, Pin, RegistrationSecret.");
            }

            // 2) sprawdź secret rejestracji
            string? expectedSecret = _config["Registration:Secret"];
            if (string.IsNullOrWhiteSpace(expectedSecret))
                return StatusCode(500, "Brak konfiguracji Registration:Secret w appsettings.json");

            if (!string.Equals(request.RegistrationSecret, expectedSecret, StringComparison.Ordinal))
                return Unauthorized("Niepoprawne hasło rejestracji.");

            // 3) unikalność deviceId
            if (await _deviceService.DeviceExistsAsync(request.DeviceId))
                return Conflict("DeviceId już istnieje (urządzenie zarejestrowane).");

            // 4) zapis do DB (hash PIN + zapis urządzenia)
            var device = await _deviceService.RegisterDeviceAsync(request);

            // 5) odpowiedź
            return Ok(new
            {
                message = "Zarejestrowano urządzenie.",
                deviceId = device.DeviceId,
                ownerName = device.OwnerName,
                status = device.Status,
                createdAt = device.CreatedAt
            });
        }

        // GET /api/auth/challenge?deviceId=...
        [HttpGet("challenge")]
        public async Task<IActionResult> GetChallenge([FromQuery] string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return BadRequest("Parametr deviceId jest wymagany.");

            var device = await _deviceService.GetByDeviceIdAsync(deviceId);
            if (device == null)
                return NotFound("Urządzenie nie istnieje.");

            if (!string.Equals(device.Status, "Active", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, $"Urządzenie ma status '{device.Status}'.");

            var challenge = _challengeStore.Create(deviceId, 32);

            return Ok(new ChallengeResponseDto
            {
                DeviceId = deviceId,
                ChallengeBase64 = Convert.ToBase64String(challenge)
            });
        }

        // POST /api/auth/verify
        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifySignatureRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.DeviceId) ||
                string.IsNullOrWhiteSpace(request.ChallengeBase64) ||
                string.IsNullOrWhiteSpace(request.SignatureBase64))
            {
                return BadRequest("Wymagane: DeviceId, ChallengeBase64, SignatureBase64.");
            }

            var device = await _deviceService.GetByDeviceIdAsync(request.DeviceId);
            if (device == null)
                return NotFound("Urządzenie nie istnieje.");

            if (!string.Equals(device.Status, "Active", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, $"Urządzenie ma status '{device.Status}'.");

            byte[] challenge;
            byte[] signature;

            try
            {
                challenge = Convert.FromBase64String(request.ChallengeBase64);
                signature = Convert.FromBase64String(request.SignatureBase64);
            }
            catch
            {
                return BadRequest("ChallengeBase64 lub SignatureBase64 nie jest poprawnym Base64.");
            }

            if (!_challengeStore.TryConsume(device.DeviceId, challenge))
                return Unauthorized("Challenge nie pasuje / nie istnieje / został już zużyty.");

            bool ok = _sigVerifier.Verify(device, challenge, signature);
            if (!ok)
                return Unauthorized("Podpis nieprawidłowy.");

            string token = _tokenService.GenerateToken();
            var session = await _tokenService.CreateSessionAsync(device, token, TimeSpan.FromMinutes(1));

            return Ok(new LoginResultDto
            {
                DeviceId = device.DeviceId,
                Token = session.Token,
                ExpiresAt = session.ExpiresAt
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token))
                return BadRequest("Token jest wymagany.");

            bool ok = await _tokenService.RevokeTokenAsync(request.Token);
            if (!ok)
                return NotFound("Sesja dla podanego tokenu nie istnieje.");

            return Ok(new { message = "Wylogowano (token unieważniony)." });
        }

        [HttpPost("pin-check")]
        public async Task<IActionResult> PinCheck([FromBody] PinCheckRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.DeviceId) ||
                string.IsNullOrWhiteSpace(request.Pin))
            {
                return BadRequest("Wymagane: DeviceId, Pin.");
            }

            var device = await _deviceService.GetByDeviceIdAsync(request.DeviceId);
            if (device == null)
                return NotFound("Urządzenie nie istnieje.");

            if (!string.Equals(device.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new PinCheckResponse
                {
                    Success = false,
                    Message = $"Urządzenie ma status '{device.Status}'.",
                    FailedAttempts = device.FailedLoginCount,
                    MaxAttempts = 5,
                    Status = device.Status
                });
            }

            bool pinOk = _pinHasher.VerifyPin(request.Pin, device.PinSalt, device.PinHash);

            if (!pinOk)
            {
                await _lockout.RegisterFailureAsync(device);

                return Unauthorized(new PinCheckResponse
                {
                    Success = false,
                    Message = device.Status == "Blocked"
                        ? "PIN nieprawidłowy. Urządzenie zostało ZABLOKOWANE."
                        : "PIN nieprawidłowy.",
                    FailedAttempts = device.FailedLoginCount,
                    MaxAttempts = 5,
                    Status = device.Status
                });
            }

            await _lockout.ResetFailuresAsync(device);

            return Ok(new PinCheckResponse
            {
                Success = true,
                Message = "PIN poprawny.",
                FailedAttempts = 0,
                MaxAttempts = 5,
                Status = device.Status
            });
        }
    }
}
