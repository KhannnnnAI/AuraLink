using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Linq;

namespace ui_avalonia.Services
{
    public class ProfileService
    {
        private string GetProfilesFilePath()
        {
            // Tìm thư mục config chứa profiles.json ở các cấp thư mục cha
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(currentDir))
            {
                string configDir = Path.Combine(currentDir, "config");
                string testPath = Path.Combine(configDir, "profiles.json");
                if (File.Exists(testPath)) return testPath;
                
                // Nếu chưa tồn tại file nhưng có thư mục config, trả về đường dẫn này luôn
                if (Directory.Exists(configDir))
                    return testPath;

                currentDir = Path.GetDirectoryName(currentDir) ?? string.Empty;
            }
            
            // Mặc định tạo thư mục config cục bộ
            string fallbackConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
            if (!Directory.Exists(fallbackConfig))
            {
                Directory.CreateDirectory(fallbackConfig);
            }
            return Path.Combine(fallbackConfig, "profiles.json");
        }

        public List<Models.IPProfile> LoadProfiles()
        {
            string filePath = GetProfilesFilePath();
            if (!File.Exists(filePath))
            {
                // Thử tìm ở thư mục cha của dự án Mang
                string parentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
                string altPath = Path.Combine(parentDir, "config", "profiles.json");
                if (File.Exists(altPath))
                {
                    filePath = altPath;
                }
                else
                {
                    return new List<Models.IPProfile>();
                }
            }

            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                    return new List<Models.IPProfile>();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Models.IPProfile>>(json, options) ?? new List<Models.IPProfile>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading profiles: {ex.Message}");
                return new List<Models.IPProfile>();
            }
        }

        public bool SaveProfiles(List<Models.IPProfile> profiles)
        {
            try
            {
                string filePath = GetProfilesFilePath();
                string dir = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(profiles, options);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving profiles: {ex.Message}");
                return false;
            }
        }

        public bool AddProfile(Models.IPProfile profile)
        {
            var list = LoadProfiles();
            // Loại bỏ profile cũ trùng tên nếu có
            list.RemoveAll(p => p.ProfileName.Equals(profile.ProfileName, StringComparison.OrdinalIgnoreCase));
            list.Add(profile);
            return SaveProfiles(list);
        }

        public bool DeleteProfile(string profileName)
        {
            var list = LoadProfiles();
            int removed = list.RemoveAll(p => p.ProfileName.Equals(profileName, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                return SaveProfiles(list);
            }
            return false;
        }

        public bool ExportProfiles(string targetPath, List<Models.IPProfile> profiles)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(profiles, options);
                File.WriteAllText(targetPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error exporting profiles: {ex.Message}");
                return false;
            }
        }

        public (int added, int skipped) ImportProfiles(string sourcePath)
        {
            if (!File.Exists(sourcePath)) return (0, 0);

            try
            {
                string json = File.ReadAllText(sourcePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var imported = JsonSerializer.Deserialize<List<Models.IPProfile>>(json, options);
                if (imported == null) return (0, 0);

                var current = LoadProfiles();
                int addedCount = 0;
                int skippedCount = 0;

                foreach (var item in imported)
                {
                    if (current.Any(p => p.ProfileName.Equals(item.ProfileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        skippedCount++;
                    }
                    else
                    {
                        current.Add(item);
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    SaveProfiles(current);
                }

                return (addedCount, skippedCount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error importing profiles: {ex.Message}");
                return (0, 0);
            }
        }
    }
}
