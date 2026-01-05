// Copyright 2026 Keyfactor
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
