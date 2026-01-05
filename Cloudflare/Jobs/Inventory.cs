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

using System.Security.Cryptography;
using Cloudflare.Exceptions;
using Cloudflare.Service;
using Keyfactor.Extensions.Orchestrator.Cloudflare.Client;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrator.CloudflareEdge.Jobs;

public class Inventory : IInventoryJobExtension
{
    private readonly ILogger _logger;
    private readonly ICertificateRetrievalService _certificateService;
    private ICloudflareClient _cloudflareClient;
    
    private const string CloudflareHost = "www.cloudflare.com";

    // Default constructor used by Universal Orchestrator
    public Inventory()
    {
        _logger = LogHandler.GetClassLogger<Inventory>();
        _certificateService = new CertificateRetrievalService(_logger);
    }

    // Constructor for dependency injection (used by unit tests)
    public Inventory(ILogger logger, ICloudflareClient cloudflareClient, ICertificateRetrievalService certificateRetrievalService)
    {
        _logger = logger;
        _cloudflareClient = cloudflareClient;
        _certificateService = certificateRetrievalService;
    }

    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.CloudflareEdge.Inventory";

    public JobResult ProcessJob(InventoryJobConfiguration jobConfiguration, SubmitInventoryUpdate submitInventoryUpdate)
    {
        ConfigureClient(jobConfiguration);
        
        return InventoryCertificateAsync(jobConfiguration, submitInventoryUpdate)
            .GetAwaiter()
            .GetResult();
    }

    private async Task<JobResult> InventoryCertificateAsync(InventoryJobConfiguration jobConfiguration,
        SubmitInventoryUpdate submitInventoryUpdate)
    {
        try
        {
            var zoneId = jobConfiguration.CertificateStoreDetails.StorePath;

            _logger.LogInformation($"Inventorying certificates for zone id {zoneId}");

            int page = 1;
            HashSet<string> domains = new();
            List<CurrentInventoryItem> inventory = new();

            while (true)
            {
                var result = await _cloudflareClient.GetCertificatePacks(zoneId, page);

                // Break out of loop if no results found
                if (result.ResultInfo.Count == 0)
                {
                    _logger.LogDebug($"No more certificate packs found after page {page - 1}");
                    break;
                }

                _logger.LogDebug($"Found {result.Result.Count} certificate packs on page {page}");

                foreach (var certificatePack in result.Result)
                {
                    foreach (var host in certificatePack.Hosts)
                    {
                        domains.Add(host);
                    }
                }

                // Break out of loop if we've processed all pages
                if (result.ResultInfo.TotalPages <= page)
                {
                    _logger.LogDebug("All pages processed");
                    break;
                }


                page++;
                _logger.LogTrace($"Continuing to next page: {page}");
            }

            var filteredHosts = FilterDomains(domains);

            // Retrieve certificates for each filtered domain
            foreach (var domain in filteredHosts)
            {
                _logger.LogDebug($"Retrieving certificate for domain: {domain}");

                var cert = await _certificateService.GetHostCertificate(domain, CloudflareHost);
                _logger.LogTrace($"Retrieved certificate: {cert.Subject}");

                var certBytes = cert.RawData;
                var pem = PemEncoding.Write("CERTIFICATE", certBytes);

                inventory.Add(new CurrentInventoryItem
                {
                    ItemStatus = OrchestratorInventoryItemStatus.Unknown,
                    PrivateKeyEntry = false,
                    Alias = domain,
                    Certificates = new List<string>()
                    {
                        new(pem),
                    },
                });
            }

            submitInventoryUpdate.Invoke(inventory);

            _logger.LogInformation($"Successfully completed inventory for zone id {zoneId}");

            _logger.MethodExit();

            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Success,
                FailureMessage = "Successfully inventoried certificates from Cloudflare Edge.",
                JobHistoryId = jobConfiguration.JobHistoryId,
            };
        }
        catch (Exception ex) when (ex is CloudflareRequestException || ex is CertificateRetrievalException)
        {
            _logger.LogError(ex, ex.Message);

            return new JobResult
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage = ex.Message,
                JobHistoryId = jobConfiguration.JobHistoryId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while inventorying certificates from Cloudflare Edge.");

            return new JobResult()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                FailureMessage = $"An unexpected error occurred while inventorying certificates from Cloudflare Edge: {ex.Message}",
                JobHistoryId = jobConfiguration.JobHistoryId,
            };
        }
        
    }

    /// <summary>
    /// Filters out unwanted domains from the list, including "sni.cloudflaressl.com" and wildcard domains.
    /// </summary>
    /// <param name="domains"></param>
    /// <returns></returns>
    private List<string> FilterDomains(ISet<string> domains)
    {
        return domains
            .Where(p => p != "sni.cloudflaressl.com"
            && !p.StartsWith("*."))
            .ToList();
    }

    private void ConfigureClient(InventoryJobConfiguration jobConfiguration)
    {
        _logger.MethodEntry();
        
        if (_cloudflareClient == null)
        {
            _logger.LogDebug($"Configuring Cloudflare client with provided API key.");
            var apiKey = jobConfiguration.ServerPassword;
            _cloudflareClient = new CloudflareClient(_logger, apiKey);
        }
        else
        {
            _logger.LogDebug($"Cloudflare client already configured.");
        }
        
        _logger.MethodExit();
    }
}
