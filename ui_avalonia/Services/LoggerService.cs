using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ui_avalonia.Services
{
    public class LoggerService
    {
        private string GetLogFilePath()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(currentDir))
            {
                string logsDir = Path.Combine(currentDir, "logs");
                string testPath = Path.Combine(logsDir, "history.log");
                if (File.Exists(testPath)) return testPath;

                if (Directory.Exists(logsDir))
                    return testPath;

                currentDir = Path.GetDirectoryName(currentDir) ?? string.Empty;
            }

            string fallbackLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(fallbackLogs))
            {
                Directory.CreateDirectory(fallbackLogs);
            }
            return Path.Combine(fallbackLogs, "history.log");
        }

        private void EnsureLogDir()
        {
            string filePath = GetLogFilePath();
            string dir = Path.GetDirectoryName(filePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public void LogChange(string @interface, string oldIp, string newIp, bool success, string errorMsg = "", string note = "")
        {
            try
            {
                EnsureLogDir();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string status = success ? "OK" : "FAIL";
                string line = $"[{timestamp}] [{status}] Interface={@interface} | {oldIp} → {newIp}";
                
                if (!string.IsNullOrEmpty(note))
                {
                    line += $" | Note={note}";
                }
                if (!success && !string.IsNullOrEmpty(errorMsg))
                {
                    line += $" | Error={errorMsg}";
                }

                File.AppendAllText(GetLogFilePath(), line + Environment.NewLine);
            }
            catch { }
        }

        public void LogDhcp(string @interface, bool success, string errorMsg = "")
        {
            try
            {
                EnsureLogDir();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string status = success ? "OK" : "FAIL";
                string line = $"[{timestamp}] [{status}] Interface={@interface} | STATIC \u2192 DHCP";

                if (!success && !string.IsNullOrEmpty(errorMsg))
                {
                    line += $" | Error={errorMsg}";
                }

                File.AppendAllText(GetLogFilePath(), line + Environment.NewLine);
            }
            catch { }
        }

        /// <summary>Ghi log tự do (dùng cho DNS Benchmark, v.v.)</summary>
        public void LogGeneric(string message)
        {
            try
            {
                EnsureLogDir();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string line = $"[{timestamp}] {message}";
                File.AppendAllText(GetLogFilePath(), line + Environment.NewLine);
            }
            catch { }
        }

        public List<string> ReadHistory(int lastN = 50)
        {
            try
            {
                string filePath = GetLogFilePath();
                if (!File.Exists(filePath)) return new List<string>();

                var lines = File.ReadAllLines(filePath);
                return lines.Skip(Math.Max(0, lines.Length - lastN)).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public bool ClearHistory()
        {
            try
            {
                string filePath = GetLogFilePath();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetLogPath()
        {
            return Path.GetFullPath(GetLogFilePath());
        }
    }
}
