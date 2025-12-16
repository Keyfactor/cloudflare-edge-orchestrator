using System.Security.Cryptography.X509Certificates;
using Cloudflare.Exceptions;
using Cloudflare.Models.Cloudflare;
using Cloudflare.Service;
using Keyfactor.Extensions.Orchestrator.Cloudflare.Client;
using Keyfactor.Extensions.Orchestrator.CloudflareEdge.Jobs;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Exception = System.Exception;

namespace Cloudflare.UnitTests;

public class InventoryTests
{
    private Mock<ICertificateRetrievalService> _mockCertificateRetrievalService = new();
    private Mock<ICloudflareClient> _mockCloudflareClient = new();
    
    private readonly Mock<SubmitInventoryUpdate> _mockSubmitInventoryUpdate = new ();
    private readonly Inventory _sut;
    
    public InventoryTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddProvider(new XUnitLoggerProvider(output, new XUnitLoggerOptions()))
                .SetMinimumLevel(LogLevel.Trace));
        var logger = loggerFactory.CreateLogger<InventoryTests>();
        
        _sut = new Inventory(logger, _mockCloudflareClient.Object, _mockCertificateRetrievalService.Object);
    }
    
    [Fact]
    public async Task ProcessJob_SuccessfulRun_ReturnsJobSuccess()
    {
        // Arrange
        var config = CreateTestConfiguration();

        var certificatePack = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo
            {
                Count = 1,
                TotalCount = 1,
                PerPage = 1,
                TotalPages = 1,
            },
            Result = new List<GetCertificatePackResultItem>
            {
                new ()
                {
                    Hosts = new List<string> { "test.example.com" },
                }
            },
        };

        var certificate = await LoadCertificateAsync("example_certificate.crt");

        _mockCloudflareClient.Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(certificatePack);
        
        _mockCertificateRetrievalService.Setup(p => p.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateX509Certificate(certificate));
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        AssertInventorySubmittedWith("test.example.com", certificate);
        AssertJobSuccess(result, "Successfully inventoried certificates from Cloudflare Edge.");
    }
    
    [Fact]
    public async Task ProcessJob_NoCertificatePacks_ReturnsJobSuccess()
    {
        // Arrange
        var config = CreateTestConfiguration();

        var certificatePack = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo
            {
                Count = 0,
                TotalCount = 0,
                PerPage = 1,
                TotalPages = 1,
            },
        };
        _mockCloudflareClient.Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(certificatePack);
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        AssertSubmittedInventoryCount(0);
        AssertJobSuccess(result, "Successfully inventoried certificates from Cloudflare Edge.");
    }
    
    [Fact]
    public async Task ProcessJob_MultiPagedResponses_ReturnsAllCertificates()
    {
        // Arrange
        var config = CreateTestConfiguration();

        var certificatePack1 = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo
            {
                Count = 1,
                TotalCount = 1,
                PerPage = 1,
                TotalPages = 2,
            },
            Result = new List<GetCertificatePackResultItem>
            {
                new ()
                {
                    Hosts = new List<string> { "test.example.com" },
                }
            },
        };
        
        var certificatePack2 = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo
            {
                Count = 1,
                TotalCount = 1,
                PerPage = 1,
                TotalPages = 2,
            },
            Result = new List<GetCertificatePackResultItem>
            {
                new ()
                {
                    Hosts = new List<string> { "foo.example.com" },
                }
            },
        };

        var certificate = await LoadCertificateAsync("example_certificate.crt");

        _mockCloudflareClient.SetupSequence(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(certificatePack1)
            .ReturnsAsync(certificatePack2);
        
        _mockCertificateRetrievalService.Setup(p => p.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateX509Certificate(certificate));
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        AssertSubmittedInventoryCount(2);
        AssertInventorySubmittedWith("test.example.com", certificate);
        AssertInventorySubmittedWith("foo.example.com", certificate);
        AssertJobSuccess(result, "Successfully inventoried certificates from Cloudflare Edge.");
    }
    
    [Theory]
    [InlineData("*.example.com")]
    [InlineData("sni.cloudflaressl.com")]
    public async Task ProcessJob_HostsIncludesExcludedDomain_DoesNotInventoryExcludedDomain(string excludedDomains)
    {
        // Arrange
        var config = CreateTestConfiguration();

        var certificatePack = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo
            {
                Count = 1,
                TotalCount = 1,
                PerPage = 1,
                TotalPages = 1,
            },
            Result = new List<GetCertificatePackResultItem>
            {
                new ()
                {
                    Hosts = new List<string> { "test.example.com", excludedDomains },
                }
            },
        };

        var certificate = await LoadCertificateAsync("example_certificate.crt");

        _mockCloudflareClient.Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(certificatePack);
        
        _mockCertificateRetrievalService.Setup(p => p.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateX509Certificate(certificate));
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        AssertSubmittedInventoryCount(1);
        AssertInventorySubmittedWith("test.example.com", certificate);
        // We should not find the excluded domain in the inventory
        AssertJobSuccess(result, "Successfully inventoried certificates from Cloudflare Edge.");
    }
    
    [Fact]
    public async Task ProcessJob_CloudflareRequestExceptionThrown_ReturnsJobFailure()
    {
        // Arrange
        var config = CreateTestConfiguration();

        var exception = new CloudflareRequestException("Whoops!");

        var certificate = await LoadCertificateAsync("example_certificate.crt");

        _mockCloudflareClient.Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(exception);
        
        _mockCertificateRetrievalService.Setup(p => p.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateX509Certificate(certificate));
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        AssertInventoryNotSubmitted();
        AssertJobFailure(result, exception.Message);
    }
    
    [Fact]
    public async Task ProcessJob_CertificateRetrievalExceptionThrown_ReturnsJobFailure()
    {
        // Arrange
        var config = CreateTestConfiguration();

        var certificatePack = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo
            {
                Count = 1,
                TotalCount = 1,
                PerPage = 1,
                TotalPages = 1,
            },
            Result = new List<GetCertificatePackResultItem>
            {
                new ()
                {
                    Hosts = new List<string> { "test.example.com" },
                }
            },
        };

        var exception = new CertificateRetrievalException("Whoops!");

        _mockCloudflareClient.Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(certificatePack);

        _mockCertificateRetrievalService.Setup(p => p.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(exception);
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        AssertInventoryNotSubmitted();
        AssertJobFailure(result, exception.Message);
    }
    
    [Fact]
    public async Task ProcessJob_UnexpectedExceptionThrown_ReturnsJobFailure()
    {
        // Arrange
        var config = CreateTestConfiguration();

        var certificatePack = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo
            {
                Count = 1,
                TotalCount = 1,
                PerPage = 1,
                TotalPages = 1,
            },
            Result = new List<GetCertificatePackResultItem>
            {
                new ()
                {
                    Hosts = new List<string> { "test.example.com" },
                }
            },
        };

        var exception = new Exception("Did not expected that to happen!");

        _mockCloudflareClient.Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(certificatePack);

        _mockCertificateRetrievalService.Setup(p => p.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(exception);
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        AssertInventoryNotSubmitted();
        AssertJobFailure(result, $"An unexpected error occurred while inventorying certificates from Cloudflare Edge: {exception.Message}");
    }
    
    #region Assertions

    private void AssertJobSuccess(JobResult result, string expectedMessage)
    {
        Assert.NotNull(result);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Success, result.Result);
        Assert.Equal(expectedMessage, result.FailureMessage);
    }
    
    private void AssertJobFailure(JobResult result, string expectedMessage)
    {
        Assert.NotNull(result);
        
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal(expectedMessage, result.FailureMessage);
    }

    private void AssertInventoryNotSubmitted()
    {
        var invocations = _mockSubmitInventoryUpdate.Invocations;
        
        // Assert no calls were made to Command
        Assert.Equal(0, invocations.Count);
    }
    
    private void AssertSubmittedInventoryCount(int expectedCount)
    {
        var invocations = _mockSubmitInventoryUpdate.Invocations;
        
        // Assert only a single update call was made to Command
        Assert.Equal(1, invocations.Count);
        
        var reportedInventory = invocations[0].Arguments[0] as List<CurrentInventoryItem>;
        
        Assert.NotNull(reportedInventory);
        
        Assert.True(expectedCount == reportedInventory!.Count, $"Expected inventory to be submitted with {expectedCount} certificate(s). Found: {reportedInventory!.Count} certificate(s).");
    }

    private void AssertInventorySubmittedWith(string expectedAlias, string expectedCertificate)
    {
        var invocations = _mockSubmitInventoryUpdate.Invocations;
        
        // Assert only a single update call was made to Command
        Assert.Equal(1, invocations.Count);
        
        var reportedInventory = invocations[0].Arguments[0] as List<CurrentInventoryItem>;
        
        Assert.NotNull(reportedInventory);
        Assert.NotEmpty(reportedInventory);
        
        var certificate = reportedInventory!.SingleOrDefault(p => p.Alias == expectedAlias);

        if (certificate == null)
        {
            Assert.True(false, $"Certificate alias {expectedAlias} not found in reported inventory. Found: {string.Join(", ", reportedInventory!.Select(p => p.Alias))}");
        }

        var match = certificate!.Certificates.SingleOrDefault(p => p == expectedCertificate);

        if (match == null)
        {
            Assert.True(false, $"Certificate data for alias {expectedAlias} does not match expected certificate. Expected {expectedCertificate}, Found: {string.Join("\n", certificate.Certificates)}");
        }
    }
    
    #endregion
    
    
    #region Test Data Helpers
    private static async Task<string> LoadCertificateAsync(string filename)
    {
        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            "fixtures",
            "example_responses",
            filename);

        return await File.ReadAllTextAsync(path);
    }

    private static X509Certificate2 CreateX509Certificate(string pem)
    {
        var base64 = pem
            .Replace("-----BEGIN CERTIFICATE-----", "")
            .Replace("-----END CERTIFICATE-----", "")
            .Replace("\r", "")
            .Replace("\n", "");

        return new X509Certificate2(Convert.FromBase64String(base64));
    }

    private static InventoryJobConfiguration CreateTestConfiguration() =>
        new()
        {
            CertificateStoreDetails = new CertificateStore
            {
                ClientMachine = "client-machine",
                StorePath = "store-path"
            },
            ServerPassword = "server-password"
        };
    
    #endregion
}
