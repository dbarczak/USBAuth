using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace USBAuthFront
{
    public class UsbLogoutService
    {
        private readonly HttpClient _http;

        public UsbLogoutService(string apiBaseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
            };
        }

        public async Task LogoutAsync(string token)
        {
            var payload = new { token = token };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync("/api/auth/logout", content);

            if (!resp.IsSuccessStatusCode)
            {
                string body = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Wylogowanie nieudane. HTTP {(int)resp.StatusCode}: {body}");
            }
        }
    }
}
