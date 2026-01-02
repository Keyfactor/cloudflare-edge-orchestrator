#nullable enable
using Newtonsoft.Json;

namespace Cloudflare.Models.Cloudflare;

public class ResponseMetadata
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")] 
    public string Message { get; set; } = string.Empty;
    
    [JsonProperty("documentation_url")]
    public string? DocumentationUrl { get; set; }
}
