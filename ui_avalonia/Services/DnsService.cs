using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace ui_avalonia.Services
{
    public class DnsService
    {
        private string GetDnsFilePath()
        {
            // Tìm file DNS.txt ở thư mục chạy hoặc các thư mục cha (để phục vụ debug)
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(currentDir))
            {
                string testPath = Path.Combine(currentDir, "DNS.txt");
                if (File.Exists(testPath)) return testPath;
                currentDir = Path.GetDirectoryName(currentDir) ?? string.Empty;
            }
            return "DNS.txt";
        }

        public List<Models.DnsRecord> ParseDnsFile()
        {
            var list = new List<Models.DnsRecord>();
            string filePath = GetDnsFilePath();

            if (!File.Exists(filePath))
            {
                // Thử tìm ở thư mục cha của dự án Mang
                string parentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
                filePath = Path.Combine(parentDir, "DNS.txt");
                if (!File.Exists(filePath))
                    return list;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string currentSection = string.Empty;

                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    // Kiểm tra Section
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(currentSection) && line.Contains('='))
                    {
                        var parts = line.Split('=', 2);
                        string name = parts[0].Trim();
                        var vals = parts[1].Split(',').Select(v => v.Trim()).ToList();

                        string primary = vals.Count > 0 ? vals[0] : string.Empty;
                        string secondary = vals.Count > 1 ? vals[1] : string.Empty;

                        if (string.IsNullOrEmpty(primary) && string.IsNullOrEmpty(secondary))
                            continue;

                        bool active = true;
                        if (vals.Count > 2)
                        {
                            active = vals[2].Equals("true", StringComparison.OrdinalIgnoreCase);
                        }

                        list.Add(new Models.DnsRecord
                        {
                            Name = name,
                            Primary = primary,
                            Secondary = secondary,
                            Section = currentSection,
                            Active = active
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading DNS.txt: {ex.Message}");
            }

            return list;
        }

        public (bool success, string error) ChangeDns(string interfaceName, string primary, string secondary, bool isIpv6 = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string proto = isIpv6 ? "ipv6" : "ipv4";
                
                // Xoá DNS cũ
                RunCommand("cmd.exe", $"/c netsh interface {proto} delete dns name=\"{interfaceName}\" all");

                if (string.IsNullOrEmpty(primary))
                {
                    // Trả về tự động DHCP
                    string cmd = $"/c netsh interface {proto} set dns name=\"{interfaceName}\" dhcp";
                    return RunCommand("cmd.exe", cmd);
                }

                // Đặt DNS chính
                string cmdPrimary = $"/c netsh interface {proto} add dns name=\"{interfaceName}\" {primary} index=1";
                var r1 = RunCommand("cmd.exe", cmdPrimary);
                if (!r1.success) return r1;

                // Đặt DNS phụ
                if (!string.IsNullOrEmpty(secondary))
                {
                    string cmdSecondary = $"/c netsh interface {proto} add dns name=\"{interfaceName}\" {secondary} index=2";
                    RunCommand("cmd.exe", cmdSecondary);
                }

                return (true, "");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Thử dùng nmcli
                bool hasNmcli = false;
                try
                {
                    var check = Process.Start(new ProcessStartInfo { FileName = "which", Arguments = "nmcli", RedirectStandardOutput = true, UseShellExecute = false });
                    check?.WaitForExit();
                    hasNmcli = check?.ExitCode == 0;
                }
                catch { }

                if (hasNmcli)
                {
                    string dnsServers = primary;
                    if (!string.IsNullOrEmpty(secondary)) dnsServers += $" {secondary}";
                    
                    string protoParam = isIpv6 ? "ipv6.dns" : "ipv4.dns";
                    string cmd = $"-c \"sudo nmcli connection modify \\\"{interfaceName}\\\" {protoParam} \\\"{dnsServers}\\\" && sudo nmcli connection up \\\"{interfaceName}\\\"\"";
                    return RunCommand("bash", cmd);
                }

                // Cách dự phòng ghi /etc/resolv.conf
                try
                {
                    var lines = new List<string>();
                    if (!string.IsNullOrEmpty(primary)) lines.Add($"nameserver {primary}");
                    if (!string.IsNullOrEmpty(secondary)) lines.Add($"nameserver {secondary}");
                    
                    if (lines.Count > 0)
                    {
                        File.AppendAllLines("/etc/resolv.conf", lines);
                    }
                    return (true, "");
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string dnsServers = string.IsNullOrEmpty(primary) ? "empty" : $"{primary} {secondary}".Trim();
                string cmd = $"-c \"sudo networksetup -setdnsservers \\\"{interfaceName}\\\" {dnsServers}\"";
                return RunCommand("bash", cmd);
            }

            return (false, "Hệ điều hành không được hỗ trợ");
        }

        private (bool success, string error) RunCommand(string filename, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return (false, "Không thể khởi chạy tiến trình.");

                process.WaitForExit();
                string err = process.StandardError.ReadToEnd().Trim();
                if (process.ExitCode == 0)
                {
                    return (true, "");
                }
                else
                {
                    return (false, string.IsNullOrEmpty(err) ? $"Mã lỗi: {process.ExitCode}" : err);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
