using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Linq;

namespace ui_avalonia.Services
{
    public class NetworkService
    {
        public List<Models.NetworkCard> GetInterfaces()
        {
            var list = new List<Models.NetworkCard>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Bỏ qua loopback và các card ảo tunnel
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                        continue;

                    // Chỉ lấy các card mạng có thể hoạt động (Ethernet hoặc Wireless)
                    if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                        continue;

                    var ipProps = ni.GetIPProperties();
                    var ipv4Props = ipProps.GetIPv4Properties();

                    string ip = "N/A";
                    string subnet = "N/A";
                    string gateway = "N/A";
                    bool isDhcp = ipv4Props?.IsDhcpEnabled ?? false;

                    var unicast = ipProps.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (unicast != null)
                    {
                        ip = unicast.Address.ToString();
                        subnet = unicast.IPv4Mask?.ToString() ?? "N/A";
                    }

                    var gw = ipProps.GatewayAddresses
                        .FirstOrDefault(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (gw != null)
                    {
                        gateway = gw.Address.ToString();
                    }

                    list.Add(new Models.NetworkCard
                    {
                        Name = ni.Name,
                        IpAddress = ip,
                        SubnetMask = subnet,
                        Gateway = gateway,
                        IsDhcp = isDhcp
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting interfaces: {ex.Message}");
            }
            return list;
        }

        public (bool success, string error) ChangeIp(string interfaceName, string ip, string subnet, string gateway)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string cmd = $"/c netsh interface ipv4 set address name=\"{interfaceName}\" static {ip} {subnet} {gateway}";
                return RunCommand("cmd.exe", cmd);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                int prefix = SubnetToPrefix(subnet);
                string cmd = $"-c \"sudo ip addr flush dev {interfaceName} && sudo ip addr add {ip}/{prefix} dev {interfaceName} && sudo ip link set {interfaceName} up && sudo ip route add default via {gateway} dev {interfaceName}\"";
                return RunCommand("bash", cmd);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string cmd = $"-c \"sudo networksetup -setmanual \\\"{interfaceName}\\\" {ip} {subnet} {gateway}\"";
                return RunCommand("bash", cmd);
            }
            return (false, "Hệ điều hành không được hỗ trợ");
        }

        public (bool success, string error) SetDhcp(string interfaceName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var r1 = RunCommand("cmd.exe", $"/c netsh interface ipv4 set address name=\"{interfaceName}\" dhcp");
                if (r1.success)
                {
                    RunCommand("cmd.exe", $"/c netsh interface ipv4 set dns name=\"{interfaceName}\" dhcp");
                }
                return r1;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string cmd = $"-c \"sudo ip addr flush dev {interfaceName} && sudo dhclient {interfaceName}\"";
                return RunCommand("bash", cmd);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string cmd = $"-c \"sudo networksetup -setdhcp \\\"{interfaceName}\\\"\"";
                return RunCommand("bash", cmd);
            }
            return (false, "Hệ điều hành không được hỗ trợ");
        }

        private int SubnetToPrefix(string subnet)
        {
            if (int.TryParse(subnet.Replace("/", ""), out int p)) return p;
            try
            {
                var parts = subnet.Split('.').Select(byte.Parse).ToArray();
                int prefix = 0;
                foreach (var part in parts)
                {
                    byte b = part;
                    while (b > 0)
                    {
                        if ((b & 1) == 1) prefix++;
                        b >>= 1;
                    }
                }
                return prefix;
            }
            catch
            {
                return 24;
            }
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
                    return (false, string.IsNullOrEmpty(err) ? $"Mã lỗi trả về: {process.ExitCode}" : err);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
