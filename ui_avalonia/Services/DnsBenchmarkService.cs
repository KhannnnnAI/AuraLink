using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentIcons.Common;

namespace ui_avalonia.Services
{
    /// <summary>Kết quả đo tốc độ của một DNS server với một domain cụ thể</summary>
    public class DnsBenchmarkResult
    {
        public string Name { get; set; } = string.Empty;
        public string PrimaryIp { get; set; } = string.Empty;
        public string SecondaryIp { get; set; } = string.Empty;

        /// <summary>Độ trễ trung bình (ms). -1 = timeout/lỗi</summary>
        public double AverageMs { get; set; } = -1;
        public double MinMs { get; set; } = -1;
        public double MaxMs { get; set; } = -1;
        public double Jitter { get; set; } = -1; // Độ ổn định (max - min)

        public int SuccessCount { get; set; }
        public int TotalAttempts { get; set; }
        public string Status { get; set; } = "Chưa đo";

        public bool IsSuccess => AverageMs >= 0;

        public double SuccessRate => TotalAttempts > 0 ? (double)SuccessCount / TotalAttempts * 100 : 0;

        public string Rating =>
            !IsSuccess ? "Lỗi" :
            AverageMs < 20 ? "Xuất sắc" :
            AverageMs < 50 ? "Tốt" :
            AverageMs < 100 ? "Trung bình" : "Kém";

        public string RatingColor =>
            !IsSuccess ? "#ef5350" :
            AverageMs < 20 ? "#00e676" :
            AverageMs < 50 ? "#4caf50" :
            AverageMs < 100 ? "#ff9800" : "#ef5350";

        /// <summary>Hiển thị thanh bar tương đối (0–100) dựa trên AverageMs</summary>
        public double RelativeBarValue { get; set; } = 0;

        /// <summary>Xếp hạng vị trí (1 là nhanh nhất)</summary>
        public int Rank { get; set; }
    }

    public class DnsBenchmarkService
    {
        private const int Rounds = 5;       // Số lần thử mỗi DNS
        private const int TimeoutMs = 3000; // Timeout mỗi query

        /// <summary>Nhóm 1: Web thông thường</summary>
        public static readonly List<(string Label, string Domain, Symbol Icon, string LogoIconKey, string LogoColor, string IconSource)> AvailableDomainsGroup1 = new()
        {
            ("Google",          "www.google.com",       Symbol.Search,      "Google",                    "#4285F4", "SimpleIcons"),
            ("Facebook",        "www.facebook.com",     Symbol.Book,        "Facebook",                  "#1877F2", "SimpleIcons"),
            ("YouTube",         "www.youtube.com",      Symbol.Play,        "YouTube",                   "#FF0000", "SimpleIcons"),
            ("TikTok",          "www.tiktok.com",       Symbol.MusicNote2,  "TikTok",                    "#FE2C55", "SimpleIcons"),
            ("Wikipedia",       "www.wikipedia.org",    Symbol.Book,        "Wikipedia",                 "#B3B3B3", "SimpleIcons"),
            ("Netflix",         "www.netflix.com",      Symbol.Play,        "Netflix",                   "#E50914", "SimpleIcons"),
            ("Twitter / X",     "www.twitter.com",      Symbol.Chat,        "X",                         "#E0E0E0", "SimpleIcons"),
            ("Instagram",       "www.instagram.com",    Symbol.Camera,      "Instagram",                 "#E1306C", "SimpleIcons"),
            ("Steam",           "store.steampowered.com",Symbol.Globe,      "Steam",                     "#66C0F4", "SimpleIcons"),
            ("Cloudflare",      "www.cloudflare.com",   Symbol.Cloud,       "Cloudflare",                "#F38020", "SimpleIcons"),
        };

        /// <summary>Nhóm 2: AI Cloud</summary>
        public static readonly List<(string Label, string Domain, Symbol Icon, string LogoIconKey, string LogoColor, string IconSource)> AvailableDomainsGroup2 = new()
        {
            ("ChatGPT",         "chatgpt.com",          Symbol.Globe,       "OpenAi",                    "#10A37F", "SimpleIcons"),
            ("Gemini",          "gemini.google.com",    Symbol.Globe,       "GoogleGemini",              "#1A73E8", "SimpleIcons"),
            ("Claude",          "claude.ai",            Symbol.Globe,       "Claude",                    "#D97706", "SimpleIcons"),
            ("Cursor",          "www.cursor.com",       Symbol.Globe,       "NVIDIA AI", "#00E5FF", "FontAwesome"),
            ("GitHub Copilot",  "copilot.github.com",   Symbol.Globe,       "GitHubCopilot",             "#8B5CF6", "SimpleIcons"),
            ("Hugging Face",    "huggingface.co",       Symbol.Globe,       "HuggingFace",               "#FFD21E", "SimpleIcons"),
            ("Perplexity",      "www.perplexity.ai",    Symbol.Globe,       "Perplexity",                "#10B981", "SimpleIcons"),
            ("ElevenLabs",      "elevenlabs.io",        Symbol.Globe,       "ElevenLabs",                "#F1E4C3", "SimpleIcons"),
            ("Poe",             "poe.com",              Symbol.Globe,       "Poe",                       "#9A3412", "SimpleIcons"),
            ("Suno",            "suno.com",             Symbol.Globe,       "Suno",                      "#FFFFFF", "SimpleIcons"),
        };

        /// <summary>Nhóm 3: Cloud Services</summary>
        public static readonly List<(string Label, string Domain, Symbol Icon, string LogoIconKey, string LogoColor, string IconSource)> AvailableDomainsGroup3 = new()
        {
            ("AWS",             "aws.amazon.com",       Symbol.Globe,       "fa-brands fa-aws",          "#FF9900", "FontAwesome"),
            ("Google Cloud",    "cloud.google.com",     Symbol.Globe,       "GoogleCloud",               "#4285F4", "SimpleIcons"),
            ("Azure",           "azure.microsoft.com",  Symbol.Globe,       "fa-brands fa-microsoft",    "#0089D6", "FontAwesome"),
            ("Cloudflare Web",  "www.cloudflare.com",   Symbol.Globe,       "Cloudflare",                "#F38020", "SimpleIcons"),
            ("DigitalOcean",    "www.digitalocean.com", Symbol.Globe,       "DigitalOcean",              "#0080FF", "SimpleIcons"),
            ("GitHub",          "github.com",           Symbol.Globe,       "GitHub",                    "#8B949E", "SimpleIcons"),
            ("GitLab",          "gitlab.com",           Symbol.Globe,       "GitLab",                    "#E24329", "SimpleIcons"),
            ("Vercel",          "vercel.com",           Symbol.Globe,       "Vercel",                    "#FFFFFF", "SimpleIcons"),
            ("Netlify",         "www.netlify.com",      Symbol.Globe,       "Netlify",                   "#00C7B7", "SimpleIcons"),
            ("Firebase",        "firebase.google.com",  Symbol.Globe,       "Firebase",                  "#FFCA28", "SimpleIcons"),
        };

        /// <summary>10 DNS server nổi tiếng</summary>
        public static readonly List<(string Name, string Primary, string Secondary)> WellKnownDns = new()
        {
            ("Cloudflare",      "1.1.1.1",          "1.0.0.1"),
            ("Google",          "8.8.8.8",           "8.8.4.4"),
            ("Quad9",           "9.9.9.9",           "149.112.112.112"),
            ("OpenDNS",         "208.67.222.222",    "208.67.220.220"),
            ("AdGuard",         "94.140.14.14",      "94.140.15.15"),
            ("NextDNS",         "45.90.28.0",        "45.90.30.0"),
            ("Comodo",          "8.26.56.26",        "8.20.247.20"),
            ("CleanBrowsing",   "185.228.168.9",     "185.228.169.9"),
            ("Alternate DNS",   "76.76.19.19",       "76.223.122.150"),
            ("DNS.WATCH",       "84.200.69.80",      "84.200.70.40"),
        };

        /// <summary>
        /// Chạy benchmark: test 1 domain được chọn với tất cả DNS server.
        /// </summary>
        public async Task<List<DnsBenchmarkResult>> RunBenchmarkAsync(
            string testDomain,
            IEnumerable<(string Name, string Primary, string Secondary)> servers,
            IProgress<(int current, int total, string message)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var serverList = servers.ToList();
            var results = new List<DnsBenchmarkResult>();
            int index = 0;

            foreach (var (name, primary, secondary) in serverList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                index++;
                progress?.Report((index, serverList.Count,
                    $"Đang đo [{index}/{serverList.Count}]: {name} ({primary})..."));

                var result = await MeasureDnsAsync(name, primary, secondary, testDomain, cancellationToken);
                results.Add(result);
            }

            // Sắp xếp tốc độ
            var sorted = results
                .OrderBy(r => !r.IsSuccess)
                .ThenBy(r => r.AverageMs)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].Rank = i + 1;
            }

            // Tính RelativeBarValue (so sánh tương đối với DNS nhanh nhất)
            var bestMs = sorted.Where(r => r.IsSuccess).Select(r => r.AverageMs).FirstOrDefault(0);
            var worstMs = sorted.Where(r => r.IsSuccess).Select(r => r.AverageMs).LastOrDefault(200);
            double range = Math.Max(worstMs - bestMs, 1);

            foreach (var r in sorted)
            {
                if (r.IsSuccess)
                    // Bar dài = nhanh (100 = tốt nhất)
                    r.RelativeBarValue = 100.0 - ((r.AverageMs - bestMs) / range * 100.0);
                else
                    r.RelativeBarValue = 0;
            }

            return sorted;
        }

        private async Task<DnsBenchmarkResult> MeasureDnsAsync(
            string name, string primary, string secondary,
            string domain, CancellationToken cancellationToken)
        {
            var result = new DnsBenchmarkResult
            {
                Name = name,
                PrimaryIp = primary,
                SecondaryIp = secondary,
                TotalAttempts = Rounds,
                Status = "Đang đo..."
            };

            var measurements = new List<double>();

            // Warm-up: 1 lần không tính
            await Task.Run(() => QueryDnsViaUdp(primary, domain), cancellationToken);
            await Task.Delay(30, cancellationToken);

            for (int i = 0; i < Rounds; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double ms = await Task.Run(() => QueryDnsViaUdp(primary, domain), cancellationToken);
                if (ms >= 0)
                {
                    measurements.Add(ms);
                    result.SuccessCount++;
                }
                if (i < Rounds - 1)
                    await Task.Delay(40, cancellationToken);
            }

            if (measurements.Count > 0)
            {
                result.AverageMs = Math.Round(measurements.Average(), 1);
                result.MinMs = Math.Round(measurements.Min(), 1);
                result.MaxMs = Math.Round(measurements.Max(), 1);
                result.Jitter = Math.Round(result.MaxMs - result.MinMs, 1);
                result.Status = $"{result.SuccessCount}/{Rounds} lần thành công";
            }
            else
            {
                result.Status = "Timeout / Không kết nối được";
            }

            return result;
        }

        private static double QueryDnsViaUdp(string dnsServerIp, string domain)
        {
            try
            {
                byte[] query = BuildDnsQuery(domain);
                using var udp = new UdpClient();
                udp.Client.ReceiveTimeout = TimeoutMs;
                udp.Client.SendTimeout = TimeoutMs;

                var endpoint = new IPEndPoint(IPAddress.Parse(dnsServerIp), 53);
                var sw = Stopwatch.StartNew();
                udp.Send(query, query.Length, endpoint);
                IPEndPoint? remoteEp = null;
                byte[] response = udp.Receive(ref remoteEp);
                sw.Stop();

                return response.Length >= 12 ? sw.Elapsed.TotalMilliseconds : -1;
            }
            catch { return -1; }
        }

        private static byte[] BuildDnsQuery(string domain)
        {
            var packet = new List<byte>();
            var id = BitConverter.GetBytes((ushort)new Random().Next(1, 65535));
            packet.Add(id[1]); packet.Add(id[0]);
            packet.Add(0x01); packet.Add(0x00); // Flags
            packet.Add(0x00); packet.Add(0x01); // QDCOUNT
            packet.Add(0x00); packet.Add(0x00); // ANCOUNT
            packet.Add(0x00); packet.Add(0x00); // NSCOUNT
            packet.Add(0x00); packet.Add(0x00); // ARCOUNT
            foreach (var part in domain.Split('.'))
            {
                packet.Add((byte)part.Length);
                foreach (char c in part) packet.Add((byte)c);
            }
            packet.Add(0x00);           // QNAME terminator
            packet.Add(0x00); packet.Add(0x01); // QTYPE = A
            packet.Add(0x00); packet.Add(0x01); // QCLASS = IN
            return packet.ToArray();
        }
    }
}
