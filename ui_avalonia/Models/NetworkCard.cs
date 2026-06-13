namespace ui_avalonia.Models
{
    public class NetworkCard
    {
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "N/A";
        public string SubnetMask { get; set; } = "N/A";
        public string Gateway { get; set; } = "N/A";
        public bool IsDhcp { get; set; }
        public string DisplayName => $"{Name} ({IpAddress})";
    }
}
