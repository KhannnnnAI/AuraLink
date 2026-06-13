using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ui_avalonia.Services
{
    /// <summary>
    /// Thông tin một máy chủ Speedtest (từ Ookla CLI --servers).
    /// </summary>
    public class SpeedtestServer
    {
        public int Id { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        /// <summary>Hiển thị trong ComboBox</summary>
        public string DisplayName => $"{Name} — {Location}, {Country}";
    }

    /// <summary>
    /// Kết quả đo tốc độ từ Ookla CLI.
    /// </summary>
    public class SpeedtestResult
    {
        public bool Success { get; set; }
        public double DownloadMbps { get; set; }
        public double UploadMbps { get; set; }
        public double PingMs { get; set; }
        public double Jitter { get; set; }
        public double PacketLoss { get; set; } = -1; // -1 = không đo được
        public string ServerName { get; set; } = "N/A";
        public string ServerLocation { get; set; } = string.Empty;
        public string Isp { get; set; } = string.Empty;
        public string ResultUrl { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    /// Trạng thái tiến trình đo tốc độ realtime.
    /// </summary>
    public class SpeedtestProgressState
    {
        public string Type { get; set; } = string.Empty; // "connecting", "testStart", "ping", "download", "upload", "result", "error"
        public double Progress { get; set; } // 0 -> 100
        public string Message { get; set; } = string.Empty;
        public double CurrentSpeedMbps { get; set; }
        public double PingMs { get; set; }
        public double JitterMs { get; set; }
        public double DownloadMbps { get; set; }
        public double UploadMbps { get; set; }
        public double PacketLoss { get; set; } = -1;
        public string ServerName { get; set; } = string.Empty;
        public string Isp { get; set; } = string.Empty;
        public string ResultUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service gọi Ookla Speedtest CLI (speedtest.exe) để đo tốc độ mạng.
    /// </summary>
    public class SpeedtestService
    {
        private readonly string _speedtestExePath;

        public SpeedtestService()
        {
            // Tìm speedtest.exe nằm cùng thư mục gốc dự án
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Truy ngược từ bin/Debug/net9.0 về thư mục gốc dự án
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            _speedtestExePath = Path.Combine(projectRoot, "ookla-speedtest-1.2.0", "speedtest.exe");

            // Fallback: thử tìm từ thư mục hiện tại
            if (!File.Exists(_speedtestExePath))
            {
                _speedtestExePath = Path.Combine(Directory.GetCurrentDirectory(), "ookla-speedtest-1.2.0", "speedtest.exe");
            }
            if (!File.Exists(_speedtestExePath))
            {
                // Fallback cuối: thử đường dẫn tuyệt đối
                _speedtestExePath = @"F:\Project Code\Mang\ookla-speedtest-1.2.0\speedtest.exe";
            }
        }

        /// <summary>
        /// Lấy danh sách máy chủ speedtest gần nhất.
        /// </summary>
        public async Task<List<SpeedtestServer>> GetServersAsync(CancellationToken ct = default)
        {
            var servers = new List<SpeedtestServer>();
            var cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "speedtest_servers_cache.json");

            // 1. Thử đọc từ cache nếu file tồn tại và chưa quá 24 giờ
            if (File.Exists(cacheFile))
            {
                try
                {
                    var fileInfo = new FileInfo(cacheFile);
                    if (DateTime.Now - fileInfo.LastWriteTime < TimeSpan.FromHours(24))
                    {
                        var cachedJson = await File.ReadAllTextAsync(cacheFile, ct);
                        var parsed = ParseServersJson(cachedJson);
                        if (parsed.Count == 0)
                            parsed = ParseHttpServersJson(cachedJson);

                        if (parsed.Count > 0)
                        {
                            return parsed;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SpeedtestService] Read cache error: {ex.Message}");
                }
            }

            // 2. Thử gọi HTTP API trực tiếp (Nhanh, không bị giới hạn số lần gọi của CLI)
            try
            {
                var httpServers = await FetchServersFromHttpApiAsync(ct);
                if (httpServers.Count > 0)
                {
                    var serialized = JsonSerializer.Serialize(httpServers);
                    await File.WriteAllTextAsync(cacheFile, serialized, ct);
                    return httpServers;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpeedtestService] Fetch HTTP API error, fallback to CLI: {ex.Message}");
            }

            // 3. Nếu HTTP API thất bại, fallback gọi CLI
            try
            {
                var output = await RunCliAsync("--servers --format=json --accept-license --accept-gdpr", ct);
                if (!string.IsNullOrWhiteSpace(output))
                {
                    var parsed = ParseServersJson(output);
                    if (parsed.Count > 0)
                    {
                        await File.WriteAllTextAsync(cacheFile, output, ct);
                        return parsed;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpeedtestService] Fetch CLI servers error: {ex.Message}");
                
                // Fallback: Nếu lỗi (ví dụ Too Many Requests), cố gắng đọc lại từ cache cũ (dù đã quá 24h)
                if (File.Exists(cacheFile))
                {
                    try
                    {
                        var cachedJson = await File.ReadAllTextAsync(cacheFile, ct);
                        var parsed = ParseServersJson(cachedJson);
                        if (parsed.Count == 0)
                            parsed = ParseHttpServersJson(cachedJson);

                        if (parsed.Count > 0)
                        {
                            return parsed;
                        }
                    }
                    catch { }
                }
            }


            return servers;
        }

        private async Task<List<SpeedtestServer>> FetchServersFromHttpApiAsync(CancellationToken ct)
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            var url = "https://www.speedtest.net/api/js/servers?engine=js&limit=20&https_functional=true";
            var responseJson = await client.GetStringAsync(url, ct);
            return ParseHttpServersJson(responseJson);
        }

        private List<SpeedtestServer> ParseHttpServersJson(string json)
        {
            var list = new List<SpeedtestServer>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in root.EnumerateArray())
                    {
                        // Parse Id (can be integer or string)
                        int id = 0;
                        if (s.TryGetProperty("id", out var idEl))
                        {
                            if (idEl.ValueKind == JsonValueKind.Number)
                                id = idEl.GetInt32();
                            else if (idEl.ValueKind == JsonValueKind.String && int.TryParse(idEl.GetString(), out int parsedId))
                                id = parsedId;
                        }
                        else if (s.TryGetProperty("Id", out var idEl2))
                        {
                            if (idEl2.ValueKind == JsonValueKind.Number)
                                id = idEl2.GetInt32();
                            else if (idEl2.ValueKind == JsonValueKind.String && int.TryParse(idEl2.GetString(), out int parsedId))
                                id = parsedId;
                        }

                        // Parse Host
                        string host = "";
                        if (s.TryGetProperty("Host", out var h1)) host = h1.GetString() ?? "";
                        else if (s.TryGetProperty("host", out var h2)) host = h2.GetString() ?? "";

                        // Parse Port
                        int port = 8080;
                        if (s.TryGetProperty("Port", out var p1)) port = p1.GetInt32();
                        else if (s.TryGetProperty("port", out var p2))
                        {
                            if (p2.ValueKind == JsonValueKind.Number) port = p2.GetInt32();
                            else if (p2.ValueKind == JsonValueKind.String && int.TryParse(p2.GetString(), out int parsedPort)) port = parsedPort;
                        }

                        // Parse Name (Sponsor in HTTP API)
                        string name = "";
                        if (s.TryGetProperty("Name", out var n1)) name = n1.GetString() ?? "";
                        else if (s.TryGetProperty("sponsor", out var sp)) name = sp.GetString() ?? "";

                        // Parse Location (Name in HTTP API)
                        string location = "";
                        if (s.TryGetProperty("Location", out var l1)) location = l1.GetString() ?? "";
                        else if (s.TryGetProperty("name", out var n2)) location = n2.GetString() ?? "";

                        // Parse Country
                        string country = "";
                        if (s.TryGetProperty("Country", out var c1)) country = c1.GetString() ?? "";
                        else if (s.TryGetProperty("country", out var c2)) country = c2.GetString() ?? "";

                        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(host))
                        {
                            list.Add(new SpeedtestServer
                            {
                                Id = id,
                                Host = host,
                                Port = port,
                                Name = name,
                                Location = location,
                                Country = country
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpeedtestService] Parse HTTP servers error: {ex.Message}");
            }
            return list;
        }

        private List<SpeedtestServer> ParseServersJson(string json)
        {
            var list = new List<SpeedtestServer>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("servers", out var serversArray))
                {
                    foreach (var s in serversArray.EnumerateArray())
                    {
                        list.Add(new SpeedtestServer
                        {
                            Id = s.GetProperty("id").GetInt32(),
                            Host = s.TryGetProperty("host", out var h) ? h.GetString() ?? "" : "",
                            Port = s.TryGetProperty("port", out var p) ? p.GetInt32() : 8080,
                            Name = s.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                            Location = s.TryGetProperty("location", out var l) ? l.GetString() ?? "" : "",
                            Country = s.TryGetProperty("country", out var c) ? c.GetString() ?? "" : "",
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpeedtestService] Parse servers error: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// Chạy speedtest với server cụ thể (hoặc tự chọn server nếu serverId == null).
        /// </summary>
        public async Task<SpeedtestResult> RunSpeedtestAsync(
            int? serverId = null,
            Action<SpeedtestProgressState>? progressCallback = null,
            CancellationToken ct = default)
        {
            var result = new SpeedtestResult();

            try
            {
                var state = new SpeedtestProgressState
                {
                    Type = "connecting",
                    Progress = 2,
                    Message = "Đang kết nối tới server..."
                };
                progressCallback?.Invoke(state);

                var args = "--format=jsonl --accept-license --accept-gdpr";
                if (serverId.HasValue)
                    args += $" --server-id={serverId.Value}";

                // Run CLI and parse stdout line by line
                var lastLine = await RunCliRealtimeAsync(args, line =>
                {
                    if (string.IsNullOrWhiteSpace(line)) return;

                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;
                        if (!root.TryGetProperty("type", out var typeEl)) return;
                        var type = typeEl.GetString();

                        if (type == "testStart")
                        {
                            state.Type = "testStart";
                            state.Progress = 5;
                            state.Message = "Bắt đầu đo tốc độ...";
                            if (root.TryGetProperty("isp", out var ispEl))
                            {
                                state.Isp = ispEl.GetString() ?? "";
                            }
                            if (root.TryGetProperty("server", out var srvEl))
                            {
                                var name = srvEl.TryGetProperty("name", out var sn) ? sn.GetString() ?? "" : "";
                                var loc = srvEl.TryGetProperty("location", out var sl) ? sl.GetString() ?? "" : "";
                                var country = srvEl.TryGetProperty("country", out var sc) ? sc.GetString() ?? "" : "";
                                state.ServerName = $"{name} — {loc}, {country}";
                            }
                            progressCallback?.Invoke(state);
                        }
                        else if (type == "ping")
                        {
                            state.Type = "ping";
                            if (root.TryGetProperty("ping", out var pingEl))
                            {
                                var lat = pingEl.TryGetProperty("latency", out var l) ? l.GetDouble() : 0;
                                var jit = pingEl.TryGetProperty("jitter", out var j) ? j.GetDouble() : 0;
                                var prog = pingEl.TryGetProperty("progress", out var p) ? p.GetDouble() : 0;

                                state.PingMs = lat;
                                state.JitterMs = jit;
                                state.Progress = 5 + prog * 5; // Ping chiếm 5% -> 10%
                                state.Message = $"Đang đo ping: {lat:F1} ms (Jitter: {jit:F1} ms)...";
                            }
                            progressCallback?.Invoke(state);
                        }
                        else if (type == "download")
                        {
                            state.Type = "download";
                            if (root.TryGetProperty("download", out var dlEl))
                            {
                                var bw = dlEl.TryGetProperty("bandwidth", out var b) ? b.GetInt64() : 0;
                                var prog = dlEl.TryGetProperty("progress", out var p) ? p.GetDouble() : 0;
                                double speed = Math.Round(bw * 8.0 / 1_000_000, 2);

                                state.CurrentSpeedMbps = speed;
                                state.DownloadMbps = speed;
                                state.Progress = 10 + prog * 38; // Download chiếm 10% -> 48%
                                state.Message = $"Đang đo tốc độ tải xuống: {speed:F2} Mbps...";
                            }
                            progressCallback?.Invoke(state);
                        }
                        else if (type == "upload")
                        {
                            state.Type = "upload";
                            if (root.TryGetProperty("upload", out var ulEl))
                            {
                                var bw = ulEl.TryGetProperty("bandwidth", out var b) ? b.GetInt64() : 0;
                                var prog = ulEl.TryGetProperty("progress", out var p) ? p.GetDouble() : 0;
                                double speed = Math.Round(bw * 8.0 / 1_000_000, 2);

                                state.CurrentSpeedMbps = speed;
                                state.UploadMbps = speed;
                                state.Progress = 50 + prog * 48; // Upload chiếm 50% -> 98%
                                state.Message = $"Đang đo tốc độ tải lên: {speed:F2} Mbps...";
                            }
                            progressCallback?.Invoke(state);
                        }
                        else if (type == "result")
                        {
                            state.Type = "result";
                            state.Progress = 99;
                            state.Message = "Đang hoàn tất...";

                            ParseResultLine(root, result);

                            state.DownloadMbps = result.DownloadMbps;
                            state.UploadMbps = result.UploadMbps;
                            state.PingMs = result.PingMs;
                            state.JitterMs = result.Jitter;
                            state.PacketLoss = result.PacketLoss;
                            state.Isp = result.Isp;
                            state.ServerName = result.ServerName;
                            state.ResultUrl = result.ResultUrl;

                            progressCallback?.Invoke(state);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SpeedtestService] Live parse error: {ex.Message}");
                    }
                }, ct);

                if (string.IsNullOrWhiteSpace(lastLine))
                {
                    result.Error = "Không nhận được kết quả từ Speedtest CLI.";
                    return result;
                }

                if (!result.Success)
                {
                    using var doc = JsonDocument.Parse(lastLine);
                    ParseResultLine(doc.RootElement, result);
                }

                state.Progress = 100;
                state.Message = "Hoàn thành!";
                progressCallback?.Invoke(state);
            }
            catch (OperationCanceledException)
            {
                result.Error = "Đo tốc độ đã bị hủy.";
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (msg.Contains("Limit reached") || msg.Contains("Too many requests"))
                {
                    msg = "Giới hạn lượt đo từ Ookla Speedtest (Rate Limit). Vui lòng thử đổi IP bằng cách bật/tắt Chế độ máy bay (nếu dùng 4G) hoặc reset Modem/Router (nếu dùng Wi-Fi), hoặc đợi vài phút rồi thử lại.";
                }
                else
                {
                    msg = $"Lỗi: {msg}";
                }

                result.Error = msg;
                var errState = new SpeedtestProgressState
                {
                    Type = "error",
                    Progress = 100,
                    Message = msg
                };
                progressCallback?.Invoke(errState);
            }

            return result;
        }

        /// <summary>
        /// Gọi speedtest.exe với tham số và trả về stdout.
        /// </summary>
        private async Task<string> RunCliAsync(string arguments, CancellationToken ct = default)
        {
            if (!File.Exists(_speedtestExePath))
                throw new FileNotFoundException($"Không tìm thấy speedtest.exe tại: {_speedtestExePath}");

            var psi = new ProcessStartInfo
            {
                FileName = _speedtestExePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(stdout))
            {
                throw new Exception($"Speedtest CLI lỗi (exit {process.ExitCode}): {stderr}");
            }

            return stdout.Trim();
        }

        /// <summary>
        /// Gọi speedtest.exe và gọi callback cho mỗi dòng stdout realtime.
        /// </summary>
        private async Task<string> RunCliRealtimeAsync(
            string arguments,
            Action<string> lineCallback,
            CancellationToken ct = default)
        {
            if (!File.Exists(_speedtestExePath))
                throw new FileNotFoundException($"Không tìm thấy speedtest.exe tại: {_speedtestExePath}");

            var psi = new ProcessStartInfo
            {
                FileName = _speedtestExePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var lastLine = string.Empty;
            var stderrTask = process.StandardError.ReadToEndAsync(ct);

            while (await process.StandardOutput.ReadLineAsync() is string line)
            {
                ct.ThrowIfCancellationRequested();
                lineCallback(line);
                lastLine = line;
            }

            await process.WaitForExitAsync(ct);
            var stderr = await stderrTask;

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(lastLine))
            {
                throw new Exception($"Speedtest CLI lỗi (exit {process.ExitCode}): {stderr}");
            }

            return lastLine.Trim();
        }

        private void ParseResultLine(JsonElement root, SpeedtestResult result)
        {
            if (root.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "result")
            {
                // Ping
                if (root.TryGetProperty("ping", out var ping))
                {
                    result.PingMs = ping.TryGetProperty("latency", out var lat) ? lat.GetDouble() : 0;
                    result.Jitter = ping.TryGetProperty("jitter", out var jit) ? jit.GetDouble() : 0;
                }

                // Download (bandwidth in bytes/s → Mbps)
                if (root.TryGetProperty("download", out var dl))
                {
                    var bw = dl.TryGetProperty("bandwidth", out var b) ? b.GetInt64() : 0;
                    result.DownloadMbps = Math.Round(bw * 8.0 / 1_000_000, 2);
                }

                // Upload (bandwidth in bytes/s → Mbps)
                if (root.TryGetProperty("upload", out var ul))
                {
                    var bw = ul.TryGetProperty("bandwidth", out var b) ? b.GetInt64() : 0;
                    result.UploadMbps = Math.Round(bw * 8.0 / 1_000_000, 2);
                }

                // Packet Loss
                if (root.TryGetProperty("packetLoss", out var plEl))
                {
                    result.PacketLoss = plEl.GetDouble();
                }

                // ISP
                if (root.TryGetProperty("isp", out var ispEl))
                {
                    result.Isp = ispEl.GetString() ?? "";
                }

                // Server info
                if (root.TryGetProperty("server", out var srv))
                {
                    var name = srv.TryGetProperty("name", out var sn) ? sn.GetString() ?? "" : "";
                    var loc = srv.TryGetProperty("location", out var sl) ? sl.GetString() ?? "" : "";
                    var country = srv.TryGetProperty("country", out var sc) ? sc.GetString() ?? "" : "";
                    result.ServerName = $"{name} — {loc}, {country}";
                    result.ServerLocation = $"{loc}, {country}";
                }

                // Result URL
                if (root.TryGetProperty("result", out var res))
                {
                    result.ResultUrl = res.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";
                }

                result.Success = true;
            }
            else
            {
                result.Error = root.TryGetProperty("message", out var msg)
                    ? msg.GetString() ?? "Lỗi không xác định"
                    : "Speedtest trả về kết quả không hợp lệ.";
            }
        }
    }
}
