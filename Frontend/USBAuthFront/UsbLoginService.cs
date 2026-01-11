using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace USBAuthFront
{
    public class UsbLoginService
    {
        private readonly HttpClient _http;

        public UsbLoginService(string apiBaseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
            };
        }

        private async Task<PinCheckResponse> PinCheckAsync(string deviceId, string pin)
        {
            var payload = new { deviceId = deviceId, pin = pin };
            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync("/api/auth/pin-check", content);
            string body = await resp.Content.ReadAsStringAsync();

            var dto = JsonSerializer.Deserialize<PinCheckResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new PinCheckResponse { Success = false, Message = body };

            if (!resp.IsSuccessStatusCode)
                dto.Success = false;

            return dto;
        }

        public async Task<LoginResultDto> LoginAsync(string usbRootPath, string pin)
        {
            if (string.IsNullOrWhiteSpace(usbRootPath))
                throw new ArgumentException("Wybierz pendrive.");

            if (string.IsNullOrWhiteSpace(pin))
                throw new ArgumentException("Podaj PIN.");

            // 1) Wczytaj blob z pendrive
            var blob = LoadKeyBlob(usbRootPath);

            var pinCheck = await PinCheckAsync(blob.DeviceId, pin);

            if (!pinCheck.Success)
            {
                throw new InvalidOperationException(
                    $"{pinCheck.Message} (Próby: {pinCheck.FailedAttempts}/{pinCheck.MaxAttempts})");
            }

            // 2) Wyprowadź klucz AES z PIN + salt2
            byte[] salt2 = Convert.FromBase64String(blob.Salt2Base64);
            byte[] aesKey = DeriveKeyFromPin(pin, salt2, iterations: 100_000, keyLen: 32);

            // 3) Odszyfruj prywatny klucz RSA (PEM)
            string privateKeyPem = DecryptPrivateKeyPem(blob, aesKey);

            // 4) Pobierz challenge z backendu
            byte[] challenge = await GetChallengeAsync(blob.DeviceId);

            // 5) Podpisz challenge kluczem prywatnym
            byte[] signature = SignChallenge(privateKeyPem, challenge);

            // 6) Wyślij podpis do backendu i odbierz token
            var login = await VerifyAsync(blob.DeviceId, challenge, signature);

            return login;
        }

        private static KeyBlob LoadKeyBlob(string usbRootPath)
        {
            string blobPath = Path.Combine(usbRootPath, "USBAuth", "keyblob.json");
            if (!File.Exists(blobPath))
                throw new FileNotFoundException("Nie znaleziono USBAuth/keyblob.json na pendrive.");

            string json = File.ReadAllText(blobPath, Encoding.UTF8);
            var blob = JsonSerializer.Deserialize<KeyBlob>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (blob == null || string.IsNullOrWhiteSpace(blob.DeviceId))
                throw new InvalidOperationException("Keyblob jest uszkodzony lub niekompletny.");

            return blob;
        }

        private static byte[] DeriveKeyFromPin(string pin, byte[] salt, int iterations, int keyLen)
        {
            using var kdf = new Rfc2898DeriveBytes(pin, salt, iterations, HashAlgorithmName.SHA256);
            return kdf.GetBytes(keyLen);
        }

        private static string DecryptPrivateKeyPem(KeyBlob blob, byte[] aesKey)
        {
            byte[] nonce = Convert.FromBase64String(blob.NonceBase64);
            byte[] ciphertext = Convert.FromBase64String(blob.CiphertextBase64);
            byte[] tag = Convert.FromBase64String(blob.TagBase64);

            byte[] plaintext = new byte[ciphertext.Length];

            try
            {
                using var aes = new AesGcm(aesKey);
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Błędny PIN lub uszkodzony keyblob (nie udało się odszyfrować klucza).");
            }

            return Encoding.UTF8.GetString(plaintext);
        }

        private async Task<byte[]> GetChallengeAsync(string deviceId)
        {
            using var resp = await _http.GetAsync($"/api/auth/challenge?deviceId={Uri.EscapeDataString(deviceId)}");
            if (!resp.IsSuccessStatusCode)
            {
                string body = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Nie udało się pobrać challenge. HTTP {(int)resp.StatusCode}: {body}");
            }

            string json = await resp.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<ChallengeResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.ChallengeBase64))
                throw new InvalidOperationException("Serwer zwrócił niepoprawny challenge.");

            return Convert.FromBase64String(dto.ChallengeBase64);
        }

        private static byte[] SignChallenge(string privateKeyPem, byte[] challenge)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem.ToCharArray());
            return rsa.SignData(challenge, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        private async Task<LoginResultDto> VerifyAsync(string deviceId, byte[] challenge, byte[] signature)
        {
            var payload = new
            {
                deviceId = deviceId,
                challengeBase64 = Convert.ToBase64String(challenge),
                signatureBase64 = Convert.ToBase64String(signature)
            };

            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync("/api/auth/verify", content);
            if (!resp.IsSuccessStatusCode)
            {
                string body = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Logowanie nieudane. HTTP {(int)resp.StatusCode}: {body}");
            }

            string respJson = await resp.Content.ReadAsStringAsync();
            var login = JsonSerializer.Deserialize<LoginResultDto>(respJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (login == null || string.IsNullOrWhiteSpace(login.Token))
                throw new InvalidOperationException("Serwer nie zwrócił tokenu.");

            return login;
        }
    }
}
