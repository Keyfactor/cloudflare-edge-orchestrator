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
    private readonly Mock<ICertificateRetrievalService> _mockCertificateRetrievalService;
    private readonly Mock<ICloudflareClient> _mockCloudflareClient;
    private readonly Mock<SubmitInventoryUpdate> _mockSubmitInventoryUpdate;
    private readonly Inventory _sut;
    private readonly string _testCertificatePem;
    
    public InventoryTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddProvider(new XUnitLoggerProvider(output, new XUnitLoggerOptions()))
                .SetMinimumLevel(LogLevel.Trace));
        var logger = loggerFactory.CreateLogger<InventoryTests>();
        
        _mockCertificateRetrievalService = new Mock<ICertificateRetrievalService>();
        _mockCloudflareClient = new Mock<ICloudflareClient>();
        _mockSubmitInventoryUpdate = new Mock<SubmitInventoryUpdate>();
        
        _sut = new Inventory(logger, _mockCloudflareClient.Object, _mockCertificateRetrievalService.Object);
        
        // Load test certificate once for all tests
        _testCertificatePem = LoadCertificate("example_certificate.crt");
    }
    
    [Fact]
    public void ProcessJob_SuccessfulRun_ReturnsJobSuccess()
    {
        // Arrange
        var config = CreateTestConfiguration();
        SetupSuccessfulCertificatePackResponse("test.example.com");
        SetupSuccessfulCertificateRetrieval();
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        result.ShouldBeSuccess("Successfully inventoried certificates from Cloudflare Edge.");
        AssertInventoryContains("test.example.com", _testCertificatePem);
    }
    
    [Fact]
    public void ProcessJob_NoCertificatePacks_ReturnsJobSuccess()
    {
        // Arrange
        var config = CreateTestConfiguration();
        SetupEmptyCertificatePackResponse();
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        result.ShouldBeSuccess("Successfully inventoried certificates from Cloudflare Edge.");
        AssertInventoryCount(0);
    }
    
    [Fact]
    public void ProcessJob_MultiPagedResponses_ReturnsAllCertificates()
    {
        // Arrange
        var config = CreateTestConfiguration();
        SetupPagedCertificatePackResponses(
            new[] { "test.example.com" },
            new[] { "foo.example.com" }
        );
        SetupSuccessfulCertificateRetrieval();
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        result.ShouldBeSuccess("Successfully inventoried certificates from Cloudflare Edge.");
        AssertInventoryCount(2);
        AssertInventoryContains("test.example.com", _testCertificatePem);
        AssertInventoryContains("foo.example.com", _testCertificatePem);
    }
    
    [Theory]
    [InlineData("*.example.com")]
    [InlineData("sni.cloudflaressl.com")]
    public void ProcessJob_HostsIncludesExcludedDomain_DoesNotInventoryExcludedDomain(string excludedDomain)
    {
        // Arrange
        var config = CreateTestConfiguration();
        SetupSuccessfulCertificatePackResponse("test.example.com", excludedDomain);
        SetupSuccessfulCertificateRetrieval();
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        result.ShouldBeSuccess("Successfully inventoried certificates from Cloudflare Edge.");
        AssertInventoryCount(1);
        AssertInventoryContains("test.example.com", _testCertificatePem);
        AssertInventoryDoesNotContain(excludedDomain);
    }
    
    [Fact]
    public void ProcessJob_OnlyWildcardAndSniDomains_ReturnsEmptyInventory()
    {
        // Arrange
        var config = CreateTestConfiguration();
        
        var certificatePack = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo { Count = 1, TotalCount = 1, PerPage = 1, TotalPages = 1 },
            Result = new List<GetCertificatePackResultItem>
            {
                new() { Hosts = new List<string> { "*.example.com", "sni.cloudflaressl.com" } }
            },
        };

        _mockCloudflareClient
            .Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(certificatePack);
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        Assert.Equal(OrchestratorJobStatusJobResult.Success, result.Result);
        AssertInventoryCount(0);
    }
    
    [Fact]
    public void ProcessJob_DuplicateHostsAcrossPages_InventoriesOnlyOnce()
    {
        // Arrange
        var config = CreateTestConfiguration();
        
        var page1 = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo { Count = 1, TotalCount = 2, PerPage = 1, TotalPages = 2 },
            Result = new List<GetCertificatePackResultItem>
            {
                new() { Hosts = new List<string> { "test.example.com", "foo.example.com" } }
            },
        };
        
        var page2 = new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo { Count = 1, TotalCount = 2, PerPage = 1, TotalPages = 2 },
            Result = new List<GetCertificatePackResultItem>
            {
                new() { Hosts = new List<string> { "test.example.com", "bar.example.com" } }
            },
        };

        _mockCloudflareClient
            .SetupSequence(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(page1)
            .ReturnsAsync(page2);
        
        SetupSuccessfulCertificateRetrieval();
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        Assert.Equal(OrchestratorJobStatusJobResult.Success, result.Result);
        AssertInventoryCount(3); // test.example.com (once), foo.example.com, bar.example.com
        
        // Verify GetHostCertificate was only called 3 times (not 4)
        _mockCertificateRetrievalService.Verify(
            x => x.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()), 
            Times.Exactly(3));
    }
    
    [Fact]
    public void ProcessJob_CloudflareRequestExceptionThrown_ReturnsJobFailure()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var exception = new CloudflareRequestException("Whoops!");
        
        _mockCloudflareClient
            .Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(exception);
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        result.ShouldBeFailure(exception.Message);
        AssertInventoryNotSubmitted();
    }
    
    [Fact]
    public void ProcessJob_CertificateRetrievalExceptionThrown_ReturnsJobFailure()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var exception = new CertificateRetrievalException("Whoops!");
        
        SetupSuccessfulCertificatePackResponse("test.example.com");
        _mockCertificateRetrievalService
            .Setup(p => p.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(exception);
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        result.ShouldBeFailure(exception.Message);
        AssertInventoryNotSubmitted();
    }
    
    [Fact]
    public void ProcessJob_UnexpectedExceptionThrown_ReturnsJobFailure()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var exception = new Exception("Did not expected that to happen!");
        
        SetupSuccessfulCertificatePackResponse("test.example.com");
        _mockCertificateRetrievalService
            .Setup(p => p.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(exception);
        
        // Act
        var result = _sut.ProcessJob(config, _mockSubmitInventoryUpdate.Object);
        
        // Assert
        result.ShouldBeFailure($"An unexpected error occurred while inventorying certificates from Cloudflare Edge: {exception.Message}");
        AssertInventoryNotSubmitted();
    }
    
    #region Mock Setup Helpers
    
    private void SetupSuccessfulCertificatePackResponse(params string[] hosts)
    {
        var response = CertificatePackResponseBuilder.Create()
            .WithHosts(hosts)
            .Build();
            
        _mockCloudflareClient
            .Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(response);
    }
    
    private void SetupEmptyCertificatePackResponse()
    {
        var response = CertificatePackResponseBuilder.CreateEmpty().Build();
        
        _mockCloudflareClient
            .Setup(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(response);
    }
    
    private void SetupPagedCertificatePackResponses(params string[][] hostsPerPage)
    {
        var sequence = _mockCloudflareClient
            .SetupSequence(p => p.GetCertificatePacks(It.IsAny<string>(), It.IsAny<int>()));
        
        foreach (var hosts in hostsPerPage)
        {
            var response = CertificatePackResponseBuilder.Create()
                .WithPageInfo(totalPages: hostsPerPage.Length)
                .WithHosts(hosts)
                .Build();
                
            sequence.ReturnsAsync(response);
        }
    }
    
    private void SetupSuccessfulCertificateRetrieval()
    {
        _mockCertificateRetrievalService
            .Setup(p => p.GetHostCertificate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(TestDataHelper.CreateX509Certificate(_testCertificatePem));
    }
    
    #endregion
    
    #region Assertions

    private void AssertInventoryNotSubmitted()
    {
        _mockSubmitInventoryUpdate.Verify(
            x => x.Invoke(It.IsAny<List<CurrentInventoryItem>>()),
            Times.Never);
    }
    
    private void AssertInventoryCount(int expectedCount)
    {
        var inventory = GetSubmittedInventory();
        Assert.Equal(expectedCount, inventory.Count);
    }

    private void AssertInventoryContains(string expectedAlias, string expectedCertificate)
    {
        var inventory = GetSubmittedInventory();
        
        var certificate = inventory.SingleOrDefault(p => p.Alias == expectedAlias);
        Assert.NotNull(certificate);
        
        Assert.Contains(expectedCertificate, certificate!.Certificates);
    }
    
    private void AssertInventoryDoesNotContain(string alias)
    {
        var inventory = GetSubmittedInventory();
        Assert.DoesNotContain(inventory, p => p.Alias == alias);
    }
    
    private List<CurrentInventoryItem> GetSubmittedInventory()
    {
        _mockSubmitInventoryUpdate.Verify(
            x => x.Invoke(It.IsAny<List<CurrentInventoryItem>>()),
            Times.Once);
        
        var invocation = _mockSubmitInventoryUpdate.Invocations.Single();
        return (List<CurrentInventoryItem>)invocation.Arguments[0];
    }
    
    #endregion
    
    #region Test Data Helpers
    
    private static string LoadCertificate(string filename)
    {
        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            "fixtures",
            "example_responses",
            filename);

        return File.ReadAllText(path);
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

#region Test Builders

internal class CertificatePackResponseBuilder
{
    private int _count = 1;
    private int _totalCount = 1;
    private int _perPage = 1;
    private int _totalPages = 1;
    private List<string> _hosts = new();
    
    public static CertificatePackResponseBuilder Create() => new();
    
    public static CertificatePackResponseBuilder CreateEmpty()
    {
        return new CertificatePackResponseBuilder
        {
            _count = 0,
            _totalCount = 0,
            _hosts = new List<string>()
        };
    }
    
    public CertificatePackResponseBuilder WithPageInfo(int count = 1, int totalCount = 1, int perPage = 1, int totalPages = 1)
    {
        _count = count;
        _totalCount = totalCount;
        _perPage = perPage;
        _totalPages = totalPages;
        return this;
    }
    
    public CertificatePackResponseBuilder WithHosts(params string[] hosts)
    {
        _hosts = hosts.ToList();
        return this;
    }
    
    public GetCertificatePacksResponse Build()
    {
        return new GetCertificatePacksResponse
        {
            ResultInfo = new ResultInfo
            {
                Count = _count,
                TotalCount = _totalCount,
                PerPage = _perPage,
                TotalPages = _totalPages,
            },
            Result = _hosts.Any() 
                ? new List<GetCertificatePackResultItem>
                {
                    new() { Hosts = _hosts }
                }
                : null
        };
    }
}

internal static class TestDataHelper
{
    public static X509Certificate2 CreateX509Certificate(string pem)
    {
        var base64 = pem
            .Replace("-----BEGIN CERTIFICATE-----", "")
            .Replace("-----END CERTIFICATE-----", "")
            .Replace("\r", "")
            .Replace("\n", "");

        return new X509Certificate2(Convert.FromBase64String(base64));
    }
}

internal static class JobResultAssertions
{
    public static void ShouldBeSuccess(this JobResult result, string expectedMessage)
    {
        Assert.NotNull(result);
        Assert.Equal(OrchestratorJobStatusJobResult.Success, result.Result);
        Assert.Equal(expectedMessage, result.FailureMessage);
    }
    
    public static void ShouldBeFailure(this JobResult result, string expectedMessage)
    {
        Assert.NotNull(result);
        Assert.Equal(OrchestratorJobStatusJobResult.Failure, result.Result);
        Assert.Equal(expectedMessage, result.FailureMessage);
    }
}

#endregion