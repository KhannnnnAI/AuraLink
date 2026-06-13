namespace ui_avalonia.Models
{
    public class DnsRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Primary { get; set; } = string.Empty;
        public string Secondary { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty; // e.g. Ipv4_Default
        public bool Active { get; set; } = true;
        public bool IsIpv6 => Section.Contains("Ipv6");

        public string ActiveText => Active ? "Hoạt động" : "Khóa";
        public string ActiveColor => Active ? "#4caf50" : "#f44336"; // Green / Red
    }
}
