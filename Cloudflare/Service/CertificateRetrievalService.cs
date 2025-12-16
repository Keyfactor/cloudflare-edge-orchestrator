#nullable enable
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Cloudflare.Service;

public class CertificateRetrievalService
{
    private readonly ILogger _logger;
    
    public CertificateRetrievalService(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task<X509Certificate2> GetCertificateAsync(
        string domain, 
        string cloudflareHost = "www.cloudflare.com")
    {
        // Resolve Cloudflare IP
        var hostEntry = await Dns.GetHostEntryAsync(cloudflareHost);
        var cloudflareIp = hostEntry.AddressList.FirstOrDefault()
            ?? throw new Exception($"Could not resolve {cloudflareHost}");

        _logger.LogDebug($"Connecting to {cloudflareIp} for domain {domain}");

        X509Certificate2? certificate = null;
        X509Chain? chain = null;

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(cloudflareIp, 443);

        await using var sslStream = new SslStream(
            tcpClient.GetStream(),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (sender, cert, certChain, errors) =>
            {
                if (cert != null)
                {
                    certificate = new X509Certificate2(cert);
                    chain = certChain;
                }
                return true; // Accept for inspection
            });

        await sslStream.AuthenticateAsClientAsync(
            targetHost: domain,
            clientCertificates: null,
            checkCertificateRevocation: false);

        if (certificate == null)
        {
            throw new Exception("Failed to retrieve certificate");
        }

        return certificate;
    }

    public async Task<X509Certificate2Collection> GetCertificateChainAsync(
        string domain,
        string cloudflareHost = "www.cloudflare.com")
    {
        var hostEntry = await Dns.GetHostEntryAsync(cloudflareHost);
        var cloudflareIp = hostEntry.AddressList.FirstOrDefault()
            ?? throw new Exception($"Could not resolve {cloudflareHost}");

        X509Certificate2Collection? chainCollection = null;

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(cloudflareIp, 443);

        using var sslStream = new SslStream(
            tcpClient.GetStream(),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (sender, cert, chain, errors) =>
            {
                if (chain != null)
                {
                    chainCollection = new X509Certificate2Collection();
                    foreach (var element in chain.ChainElements)
                    {
                        chainCollection.Add(element.Certificate);
                    }
                }
                return true;
            });

        await sslStream.AuthenticateAsClientAsync(
            targetHost: domain,
            clientCertificates: null,
            checkCertificateRevocation: false);

        return chainCollection ?? new X509Certificate2Collection();
    }
}
