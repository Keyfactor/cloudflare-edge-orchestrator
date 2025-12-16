using Newtonsoft.Json;

namespace Cloudflare.Models.Cloudflare;

public class ResultInfo
{
    [JsonProperty("page")]
    public int Page { get; set; }
    
    [JsonProperty("per_page")]
    public int PerPage { get; set; }
    
    [JsonProperty("total_pages")]
    public int TotalPages { get; set; }
    
    [JsonProperty("count")]
    public int Count { get; set; }
    
    [JsonProperty("total_count")]
    public int TotalCount { get; set; }
}