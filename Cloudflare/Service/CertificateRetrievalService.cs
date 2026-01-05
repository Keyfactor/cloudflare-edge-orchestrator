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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Cloudflare.Exceptions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;

namespace Cloudflare.Service;

public class CertificateRetrievalService : ICertificateRetrievalService
{
    private readonly ILogger _logger;
    
    public CertificateRetrievalService(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task<X509Certificate2> GetHostCertificate(
        string domain, 
        string cloudflareHost = "www.cloudflare.com")
    {
        try
        {
            _logger.MethodEntry();
            _logger.LogDebug($"Getting host certificate for domain {domain} via Cloudflare host {cloudflareHost}");

            // Resolve Cloudflare IP
            var hostEntry = await Dns.GetHostEntryAsync(cloudflareHost);
            var cloudflareIp = hostEntry.AddressList.FirstOrDefault()
                               ?? throw new Exception($"Could not resolve address list from host {cloudflareHost}");

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
                throw new CertificateRetrievalException(domain);
            }

            _logger.MethodExit();

            return certificate;
        }
        catch (CertificateRetrievalException)
        {
            // Rethrow known exceptions
            throw;
        }
        catch (Exception ex)
        {
            throw new CertificateRetrievalException(domain, ex);
        }
    }

    // public async Task<X509Certificate2Collection> GetCertificateChainAsync(
    //     string domain,
    //     string cloudflareHost = "www.cloudflare.com")
    // {
    //     var hostEntry = await Dns.GetHostEntryAsync(cloudflareHost);
    //     var cloudflareIp = hostEntry.AddressList.FirstOrDefault()
    //         ?? throw new Exception($"Could not resolve {cloudflareHost}");
    //
    //     X509Certificate2Collection? chainCollection = null;
    //
    //     using var tcpClient = new TcpClient();
    //     await tcpClient.ConnectAsync(cloudflareIp, 443);
    //
    //     using var sslStream = new SslStream(
    //         tcpClient.GetStream(),
    //         leaveInnerStreamOpen: false,
    //         userCertificateValidationCallback: (sender, cert, chain, errors) =>
    //         {
    //             if (chain != null)
    //             {
    //                 chainCollection = new X509Certificate2Collection();
    //                 foreach (var element in chain.ChainElements)
    //                 {
    //                     chainCollection.Add(element.Certificate);
    //                 }
    //             }
    //             return true;
    //         });
    //
    //     await sslStream.AuthenticateAsClientAsync(
    //         targetHost: domain,
    //         clientCertificates: null,
    //         checkCertificateRevocation: false);
    //
    //     return chainCollection ?? new X509Certificate2Collection();
    // }
}
