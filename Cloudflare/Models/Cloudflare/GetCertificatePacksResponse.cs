#nullable enable
using Newtonsoft.Json;

namespace Cloudflare.Models.Cloudflare;

public class GetCertificatePacksResponse
{
    [JsonProperty("result")] 
    public List<GetCertificatePackResultItem> Result { get; set; } = new();

    [JsonProperty("result_info")] 
    public ResultInfo ResultInfo { get; set; } = new();

    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("errors")] 
    public List<ResponseMetadata> Errors { get; set; } = new();
    
    [JsonProperty("messages")]
    public List<ResponseMetadata> Messages { get; set; } = new();
}

public class GetCertificatePackResultItem
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("type")] 
    public string Type { get; set; } = string.Empty;
    
    [JsonProperty("hosts")]
    public List<string> Hosts { get; set; } = new();
    
    [JsonProperty("primary_certificate")]
    public Guid PrimaryCertificate { get; set; }

    [JsonProperty("status")] 
    public string Status { get; set; } = string.Empty;

    [JsonProperty("certificates")] 
    public List<GetCertificatePacksCertificateItem> Certificates { get; set; } = new();
    
    [JsonProperty("created_on")]
    public DateTimeOffset CreatedOn { get; set; }
    
    [JsonProperty("validity_days")]
    public int ValidityDays { get; set; }

    [JsonProperty("certificate_authority")]
    public string CertificateAuthority { get; set; } = string.Empty;

}
