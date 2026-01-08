using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace USBAuthFront
{
    public class ProtectedApiService
    {
        private readonly HttpClient _http;

        public ProtectedApiService(string apiBaseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
            };
        }

        public async Task<string> PingAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Brak tokenu – zaloguj się ponownie.");

            var req = new HttpRequestMessage(HttpMethod.Get, "/api/protected/ping");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                return $"HTTP {(int)resp.StatusCode}: {body}";
            }

            return body;
        }
    }
}
