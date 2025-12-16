using Keyfactor.Extensions.Orchestrator.CloudflareEdge.Jobs;
using Keyfactor.Orchestrators.Extensions;
using Moq;
using Xunit;

namespace Cloudflare.UnitTests;

public class InventoryTests
{
    private readonly Mock<SubmitInventoryUpdate> _mockSubmitInventoryUpdate = new ();
    private readonly Inventory _sut;
    
    public InventoryTests()
    {
        _sut = new Inventory();
    }
    
    [Fact]
    public void ProcessJob()
    {
        var config = new InventoryJobConfiguration()
        {
            CertificateStoreDetails = new CertificateStore()
            {
                StorePath = "",
            },
            ServerPassword = "",
        };
        _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
    }
}
