using Cloudflare.Models.Cloudflare;

namespace Keyfactor.Extensions.Orchestrator.Cloudflare.Client;

public interface ICloudflareClient
{
    Task<GetCertificatePacksResponse> GetCertificatePacks(string zoneId, int page);
}
