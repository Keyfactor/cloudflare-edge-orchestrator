#nullable enable
using Newtonsoft.Json;

namespace Cloudflare.Models.Cloudflare;

public class GetCertificatePacksCertificateItem
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("hosts")] 
    public List<string> Hosts { get; set; } = new();
    
    [JsonProperty("issuer")]
    public string? Issuer { get; set; }
    
    [JsonProperty("signature")]
    public string? Signature { get; set; }

    [JsonProperty("status")] 
    public string Status { get; set; } = string.Empty;
    
    [JsonProperty("bundle_method")]
    public string? BundleMethod { get; set; }
    
    [JsonProperty("zone_id")]
    public string? ZoneId { get; set; }
    
    [JsonProperty("uploaded_on")]
    public DateTimeOffset? UploadedOn { get; set; }
    
    [JsonProperty("modified_on")]
    public DateTimeOffset? ModifiedOn { get; set; }
    
    [JsonProperty("expires_on")]
    public DateTimeOffset? ExpiresOn { get; set; }
}
