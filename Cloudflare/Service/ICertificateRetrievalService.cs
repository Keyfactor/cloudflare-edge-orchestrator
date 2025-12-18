using System.Security.Cryptography.X509Certificates;

namespace Cloudflare.Service;

public interface ICertificateRetrievalService
{
    Task<X509Certificate2> GetHostCertificate(
        string domain, 
        string cloudflareHost = "www.cloudflare.com");
}
