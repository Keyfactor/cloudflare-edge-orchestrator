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

using System.Net.Http.Headers;
using Cloudflare.Exceptions;
using Cloudflare.Models.Cloudflare;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Orchestrator.Cloudflare.Client
{
    public class CloudflareClient : ICloudflareClient
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        
        public CloudflareClient(ILogger logger, string apiKey)
        {
            _logger = logger;

            _logger.MethodEntry();

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.cloudflare.com");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            
            _logger.LogTrace($"Authorization header: Bearer {apiKey}");
            
            _logger.LogDebug($"Cloudflare HTTP client initialized with base address {_httpClient.BaseAddress.AbsoluteUri}");

            _logger.MethodExit();
        }

        public async Task<GetCertificatePacksResponse> GetCertificatePacks(string zoneId, int page)
        {
            _logger.MethodEntry();
            
            string endpoint = $"client/v4/zones/{zoneId}/ssl/certificate_packs?page={page}";
            
            _logger.LogDebug($"Getting certificate packs for zone {zoneId} from endpoint {endpoint}. Page: {page}");

            string content = null;
            
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                var response = await _httpClient.SendAsync(request);
                
                content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new CloudflareRequestException($"Failed to get certificate packs. Endpoint {endpoint} returned status code {response.StatusCode}. Error content: {content}");
                }
                
                _logger.LogTrace($"Endpoint {endpoint} returned status code {response.StatusCode}");
            }
            catch (Exception ex) when (ex is not CloudflareRequestException)
            {
                throw new CloudflareRequestException(ex);
            }
            
            _logger.LogTrace($"Response content returned: {content}");

            var result = JsonConvert.DeserializeObject<GetCertificatePacksResponse>(content);
            
            _logger.LogDebug($"Successfully returned certificate packs for zone {zoneId}");
            
            _logger.MethodExit();

            return result;
        }
    }
}
