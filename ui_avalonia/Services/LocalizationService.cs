using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ui_avalonia.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        public static LocalizationService Instance { get; } = new LocalizationService();

        private string _currentLang = "vi";
        public string CurrentLang
        {
            get => _currentLang;
            set
            {
                if (_currentLang != value)
                {
                    _currentLang = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); // Cập nhật toàn bộ property
                }
            }
        }

        public string this[string key]
        {
            get
            {
                if (Translations.TryGetValue(CurrentLang, out var dict) && dict.TryGetValue(key, out var val))
                {
                    return val;
                }
                return key;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["vi"] = new()
            {
                ["menu_title"] = "MENU CHÍNH",
                ["menu_dashboard"] = "Trang chủ",
                ["menu_change_ip"] = "Đổi IP thủ công",
                ["menu_apply_profile"] = "Áp dụng profile",
                ["menu_manage_profiles"] = "Quản lý profile",
                ["menu_dhcp"] = "Đặt lại DHCP",
                ["menu_scheduler"] = "Lên lịch tự động",
                ["menu_test"] = "Kiểm tra kết nối",
                ["menu_speedtest"] = "Đo tốc độ mạng",
                ["menu_history"] = "Lịch sử & Cài đặt",
                ["menu_interfaces"] = "Xem card mạng",
                ["menu_dns"] = "Cấu hình DNS",
                ["menu_dns_benchmark"] = "Đo Tốc Độ DNS",
                ["menu_geoip"] = "Vị trí địa lý IP",
                ["menu_lang"] = "Ngôn ngữ",
                ["menu_tools"] = "Đo tốc độ",
                ["menu_exit"] = "Thoát",
                
                ["app_title"] = "Hệ thống Đổi IP & DNS Tự động",
                ["iface_select"] = "Chọn giao tiếp mạng:",
                ["ip_address"] = "Địa chỉ IP",
                ["subnet_mask"] = "Subnet Mask",
                ["gateway"] = "Default Gateway",
                ["dns_primary"] = "DNS Server chính",
                ["dns_secondary"] = "DNS Server phụ",
                ["apply"] = "Áp dụng cấu hình",
                ["restore_dhcp"] = "Chuyển về DHCP",
                
                ["profile_save"] = "Lưu cấu hình hiện tại làm Profile",
                ["profile_name"] = "Tên Profile",
                ["profile_note"] = "Ghi chú (tùy chọn)",
                ["profile_export"] = "Xuất Profile",
                ["profile_import"] = "Nhập Profile",
                ["profile_list"] = "Danh sách Profile đã lưu",
                
                ["speed_title"] = "Kết Quả Đo Tốc Độ",
                ["speed_subtitle"] = "Sử dụng Ookla Speedtest CLI — chọn nhà mạng/server và đo chính xác.",
                ["speed_select_server"] = "Chọn Máy Chủ Đo Tốc Độ",
                ["speed_load_servers"] = "Tải danh sách",
                ["speed_dl"] = "Tốc độ tải xuống (Download)",
                ["speed_ul"] = "Tốc độ tải lên (Upload)",
                ["speed_ping"] = "Độ trễ (Ping)",
                ["speed_server"] = "Máy chủ kiểm thử:",
                ["speed_isp"] = "Nhà mạng (ISP):",
                ["speed_run"] = "Bắt đầu đo",
                ["speed_jitter"] = "Jitter",
                ["speed_packet_loss"] = "Packet Loss",
                ["speed_result_url"] = "Xem kết quả trên Speedtest.net",
                
                ["geoip_title"] = "Thông Tin IP Public",
                ["geoip_ip"] = "Địa chỉ IP Public",
                ["geoip_country"] = "Quốc gia",
                ["geoip_region"] = "Vùng / Tỉnh thành",
                ["geoip_city"] = "Thành phố",
                ["geoip_isp"] = "Nhà cung cấp (ISP)",
                ["geoip_gps"] = "Tọa độ GPS",
                ["geoip_tz"] = "Múi giờ",
                ["geoip_query"] = "Tra cứu vị trí",
                
                ["history_title"] = "Nhật Ký Thay Đổi IP",
                ["history_clear"] = "Xoá lịch sử",
                ["settings_lang"] = "Cài đặt Ngôn ngữ",
                ["settings_theme"] = "Chế độ giao diện",
                ["theme_dark"] = "Giao diện Tối",
                ["theme_light"] = "Giao diện Sáng",
                ["theme_system"] = "Theo hệ thống",
                
                ["ping_gateway"] = "Ping Gateway",
                ["ping_internet"] = "Ping Internet (8.8.8.8)",
                ["ping_dns"] = "Phân giải DNS (google.com)",
                ["ping_run"] = "Kiểm tra kết nối nhanh",
                
                ["success"] = "THÀNH CÔNG",
                ["error"] = "LỖI",
                ["warning"] = "CẢNH BÁO",
                ["info"] = "THÔNG TIN"
            },
            ["en"] = new()
            {
                ["menu_title"] = "MAIN MENU",
                ["menu_dashboard"] = "Dashboard",
                ["menu_change_ip"] = "Change IP",
                ["menu_apply_profile"] = "Apply Profile",
                ["menu_manage_profiles"] = "Profiles",
                ["menu_dhcp"] = "Reset to DHCP",
                ["menu_scheduler"] = "Auto Scheduler",
                ["menu_test"] = "Ping Connection",
                ["menu_speedtest"] = "Speedtest",
                ["menu_history"] = "History & Settings",
                ["menu_interfaces"] = "Interfaces",
                ["menu_dns"] = "Configure DNS",
                ["menu_dns_benchmark"] = "DNS Speed Test",
                ["menu_geoip"] = "IP Location",
                ["menu_lang"] = "Language",
                ["menu_tools"] = "Speed Test",
                ["menu_exit"] = "Exit",

                ["app_title"] = "Auto IP & DNS System",
                ["iface_select"] = "Select Interface:",
                ["ip_address"] = "IP Address",
                ["subnet_mask"] = "Subnet Mask",
                ["gateway"] = "Default Gateway",
                ["dns_primary"] = "Primary DNS Server",
                ["dns_secondary"] = "Secondary DNS Server",
                ["apply"] = "Apply Config",
                ["restore_dhcp"] = "Restore DHCP",

                ["profile_save"] = "Save Current IP to Profile",
                ["profile_name"] = "Profile Name",
                ["profile_note"] = "Note (optional)",
                ["profile_export"] = "Export Profiles",
                ["profile_import"] = "Import Profiles",
                ["profile_list"] = "Saved Profiles List",

                ["speed_title"] = "Speedtest Results",
                ["speed_subtitle"] = "Powered by Ookla Speedtest CLI — select a server and measure accurately.",
                ["speed_select_server"] = "Select Speedtest Server",
                ["speed_load_servers"] = "Load servers",
                ["speed_dl"] = "Download Speed",
                ["speed_ul"] = "Upload Speed",
                ["speed_ping"] = "Latency (Ping)",
                ["speed_server"] = "Test Server:",
                ["speed_isp"] = "ISP Provider:",
                ["speed_run"] = "Run Speedtest",
                ["speed_jitter"] = "Jitter",
                ["speed_packet_loss"] = "Packet Loss",
                ["speed_result_url"] = "View result on Speedtest.net",

                ["geoip_title"] = "Public IP Location",
                ["geoip_ip"] = "Public IP Address",
                ["geoip_country"] = "Country",
                ["geoip_region"] = "Region / State",
                ["geoip_city"] = "City",
                ["geoip_isp"] = "ISP Provider",
                ["geoip_gps"] = "GPS Coordinates",
                ["geoip_tz"] = "Timezone",
                ["geoip_query"] = "Query Location",

                ["history_title"] = "IP Change Logs",
                ["history_clear"] = "Clear History Logs",
                ["settings_lang"] = "Language Settings",
                ["settings_theme"] = "Interface Theme",
                ["theme_dark"] = "Dark Theme",
                ["theme_light"] = "Light Theme",
                ["theme_system"] = "System Default",

                ["ping_gateway"] = "Ping Gateway",
                ["ping_internet"] = "Ping Internet (8.8.8.8)",
                ["ping_dns"] = "DNS Resolution (google.com)",
                ["ping_run"] = "Quick Connectivity Test",

                ["success"] = "SUCCESS",
                ["error"] = "ERROR",
                ["warning"] = "WARNING",
                ["info"] = "INFO"
            }
        };
    }
}
