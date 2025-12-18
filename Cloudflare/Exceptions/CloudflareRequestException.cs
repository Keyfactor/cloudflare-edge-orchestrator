namespace Cloudflare.Exceptions;

public class CloudflareRequestException : Exception
{
    public CloudflareRequestException(string message) : base($"An error occurred sending request to Cloudflare: {message}")
    {
    }

    public CloudflareRequestException(Exception ex) : base("An error occurred sending request to Cloudflare", ex)
    {
        
    }
}
