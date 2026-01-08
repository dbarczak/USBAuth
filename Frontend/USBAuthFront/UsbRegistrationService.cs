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
    public class UsbRegistrationService
    {
        private readonly HttpClient _http;

        public UsbRegistrationService(string apiBaseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
            };
        }

        public async Task RegisterAsync(string usbRootPath, string ownerName, string pin, string registrationSecret)
        {
            if (string.IsNullOrWhiteSpace(usbRootPath)) throw new ArgumentException("Wybierz pendrive.");
            if (string.IsNullOrWhiteSpace(ownerName)) throw new ArgumentException("Podaj nazwę urządzenia.");
            if (string.IsNullOrWhiteSpace(pin)) throw new ArgumentException("Podaj PIN.");
            if (pin.Length < 4) throw new ArgumentException("PIN powinien mieć min. 4 znaki.");
            if (string.IsNullOrWhiteSpace(registrationSecret)) throw new ArgumentException("Podaj hasło rejestracji.");

            // 1) DeviceId
            string deviceId = Guid.NewGuid().ToString();

            // 2) RSA keypair
            using RSA rsa = RSA.Create(2048);
            string publicKeyPem = ExportPublicKeyPem(rsa);
            string privateKeyPem = ExportPrivateKeyPem(rsa);

            // 3) KDF(PIN, salt2) -> aesKey
            byte[] salt2 = RandomNumberGenerator.GetBytes(16);
            byte[] aesKey = DeriveKeyFromPin(pin, salt2, iterations: 100_000, keyLen: 32);

            // 4) AES-GCM encrypt(privateKeyPem)
            byte[] plaintext = Encoding.UTF8.GetBytes(privateKeyPem);
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];

            using (var aes = new AesGcm(aesKey))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            // 5) Save keyblob to pendrive
            string folder = Path.Combine(usbRootPath, "USBAuth");
            Directory.CreateDirectory(folder);

            string blobPath = Path.Combine(folder, "keyblob.json");
            if (File.Exists(blobPath))
                throw new InvalidOperationException("Na tym pendrive istnieje już USBAuth/keyblob.json (zarejestrowane urządzenie).");

            var blob = new KeyBlob
            {
                Version = 1,
                DeviceId = deviceId,
                Salt2Base64 = Convert.ToBase64String(salt2),
                NonceBase64 = Convert.ToBase64String(nonce),
                CiphertextBase64 = Convert.ToBase64String(ciphertext),
                TagBase64 = Convert.ToBase64String(tag)
            };

            File.WriteAllText(blobPath, JsonSerializer.Serialize(blob, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);

            // 6) POST /api/auth/register (backend zapisuje do bazy)
            var payload = new
            {
                deviceId = deviceId,
                ownerName = ownerName,
                publicKeyPem = publicKeyPem,
                pin = pin,
                registrationSecret = registrationSecret
            };

            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync("/api/Auth/register", content);

            if (!resp.IsSuccessStatusCode)
            {
                try { File.Delete(blobPath); } catch { }

                string body = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Rejestracja w API nie powiodła się. HTTP {(int)resp.StatusCode}: {body}");
            }
        }

        private static byte[] DeriveKeyFromPin(string pin, byte[] salt, int iterations, int keyLen)
        {
            using var kdf = new Rfc2898DeriveBytes(pin, salt, iterations, HashAlgorithmName.SHA256);
            return kdf.GetBytes(keyLen);
        }

        private static string ExportPublicKeyPem(RSA rsa)
        {
            byte[] bytes = rsa.ExportSubjectPublicKeyInfo();
            string base64 = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN PUBLIC KEY-----\n{base64}\n-----END PUBLIC KEY-----";
        }

        private static string ExportPrivateKeyPem(RSA rsa)
        {
            byte[] bytes = rsa.ExportPkcs8PrivateKey();
            string base64 = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN PRIVATE KEY-----\n{base64}\n-----END PRIVATE KEY-----";
        }
    }
}
