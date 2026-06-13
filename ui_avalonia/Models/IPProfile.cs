using System.Text.Json.Serialization;

namespace ui_avalonia.Models
{
    public class IPProfile
    {
        [JsonPropertyName("profile_name")]
        public string ProfileName { get; set; } = string.Empty;

        [JsonPropertyName("interface")]
        public string Interface { get; set; } = string.Empty;

        [JsonPropertyName("ip")]
        public string Ip { get; set; } = string.Empty;

        [JsonPropertyName("subnet")]
        public string Subnet { get; set; } = string.Empty;

        [JsonPropertyName("gateway")]
        public string Gateway { get; set; } = string.Empty;

        [JsonPropertyName("dns1")]
        public string Dns1 { get; set; } = string.Empty;

        [JsonPropertyName("dns2")]
        public string Dns2 { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;
    }
}
