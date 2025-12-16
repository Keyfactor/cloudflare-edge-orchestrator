using System.Security.Cryptography;
using Cloudflare.Service;
using Keyfactor.Extensions.Orchestrator.Cloudflare.Client;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Orchestrator.CloudflareEdge.Jobs;

public class Inventory : IInventoryJobExtension
{
    private readonly ILogger _logger;
    private readonly CertificateRetrievalService _service;
    private CloudflareClient _cloudflareClient;
    
    private const string CloudflareHost = "www.cloudflare.com";

    public Inventory()
    {
        _logger = LogHandler.GetClassLogger<Inventory>();
        _service = new CertificateRetrievalService(_logger);
    }

    public string ExtensionName => "Keyfactor.Extensions.Orchestrator.CloudflareEdge.Inventory";

    public JobResult ProcessJob(InventoryJobConfiguration jobConfiguration, SubmitInventoryUpdate submitInventoryUpdate)
    {
        ConfigureClient(jobConfiguration);
        
        InventoryCertificateAsync(jobConfiguration, submitInventoryUpdate)
            .GetAwaiter()
            .GetResult();

        return new JobResult()
        {
            Result = OrchestratorJobStatusJobResult.Success,
            FailureMessage = "Successfully inventoried certificates from Cloudflare Edge.",
            JobHistoryId = jobConfiguration.JobHistoryId,
        };
    }

    private async Task InventoryCertificateAsync(InventoryJobConfiguration jobConfiguration,
        SubmitInventoryUpdate submitInventoryUpdate)
    {
        var zoneId = jobConfiguration.CertificateStoreDetails.StorePath;
        
        _logger.LogInformation($"Inventorying certificates for zone id {zoneId}");
        
        int page = 1;
        HashSet<string> domains = new();
        List<CurrentInventoryItem> inventory = new ();
        
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
            
            break;
        }

        var filteredHosts = FilterDomains(domains);

        // Retrieve certificates for each filtered domain
        foreach (var domain in filteredHosts)
        {
            _logger.LogDebug($"Retrieving certificate for domain: {domain}");
            
            var cert = await _service.GetCertificateAsync(domain, CloudflareHost);
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
        var apiKey = jobConfiguration.ServerPassword;
        _cloudflareClient = new CloudflareClient(_logger, apiKey);
    }
}
