namespace Cloudflare.Exceptions;

public class CertificateRetrievalException : Exception
{
    public CertificateRetrievalException(string hostname) : base("Failed to retrieve the server certificate for host: " + hostname)
    {
    }

    public CertificateRetrievalException(string host, Exception innerException): base($"An error occurred while retrieving the server certificate for host {host}", innerException)
    {
        
    }
}
