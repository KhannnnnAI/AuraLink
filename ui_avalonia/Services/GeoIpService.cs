using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ui_avalonia.Services
{
    public class GeoIpService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static GeoIpService()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<GeoIpInfo> GetLocationInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://ipinfo.io/json");
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                string lat = "N/A";
                string lon = "N/A";
                if (root.TryGetProperty("loc", out var locProp))
                {
                    string loc = locProp.GetString() ?? "";
                    if (loc.Contains(','))
                    {
                        var parts = loc.Split(',');
                        lat = parts[0].Trim();
                        lon = parts[1].Trim();
                    }
                }

                return new GeoIpInfo
                {
                    Success = true,
                    Ip = root.TryGetProperty("ip", out var ip) ? ip.GetString() ?? "N/A" : "N/A",
                    Country = root.TryGetProperty("country", out var c) ? c.GetString() ?? "N/A" : "N/A",
                    Region = root.TryGetProperty("region", out var r) ? r.GetString() ?? "N/A" : "N/A",
                    City = root.TryGetProperty("city", out var ct) ? ct.GetString() ?? "N/A" : "N/A",
                    Isp = root.TryGetProperty("org", out var o) ? o.GetString() ?? "N/A" : "N/A",
                    Timezone = root.TryGetProperty("timezone", out var tz) ? tz.GetString() ?? "N/A" : "N/A",
                    Latitude = lat,
                    Longitude = lon
                };
            }
            catch
            {
                return await GetLocationInfoFallbackAsync();
            }
        }

        private async Task<GeoIpInfo> GetLocationInfoFallbackAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("http://ip-api.com/json/?fields=status,message,country,regionName,city,timezone,isp,query,lat,lon");
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                string status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
                if (status == "success")
                {
                    return new GeoIpInfo
                    {
                        Success = true,
                        Ip = root.TryGetProperty("query", out var ip) ? ip.GetString() ?? "N/A" : "N/A",
                        Country = root.TryGetProperty("country", out var c) ? c.GetString() ?? "N/A" : "N/A",
                        Region = root.TryGetProperty("regionName", out var r) ? r.GetString() ?? "N/A" : "N/A",
                        City = root.TryGetProperty("city", out var ct) ? ct.GetString() ?? "N/A" : "N/A",
                        Isp = root.TryGetProperty("isp", out var o) ? o.GetString() ?? "N/A" : "N/A",
                        Timezone = root.TryGetProperty("timezone", out var tz) ? tz.GetString() ?? "N/A" : "N/A",
                        Latitude = root.TryGetProperty("lat", out var latVal) ? latVal.GetDouble().ToString() : "N/A",
                        Longitude = root.TryGetProperty("lon", out var lonVal) ? lonVal.GetDouble().ToString() : "N/A"
                    };
                }
                else
                {
                    string msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "Lỗi không xác định" : "Lỗi không xác định";
                    return new GeoIpInfo { Success = false, Error = msg };
                }
            }
            catch (Exception ex)
            {
                return new GeoIpInfo { Success = false, Error = ex.Message };
            }
        }
    }

    public class GeoIpInfo
    {
        public bool Success { get; set; }
        public string Ip { get; set; } = "N/A";
        public string Country { get; set; } = "N/A";
        public string Region { get; set; } = "N/A";
        public string City { get; set; } = "N/A";
        public string Isp { get; set; } = "N/A";
        public string Timezone { get; set; } = "N/A";
        public string Latitude { get; set; } = "N/A";
        public string Longitude { get; set; } = "N/A";
        public string Error { get; set; } = string.Empty;
    }
}
