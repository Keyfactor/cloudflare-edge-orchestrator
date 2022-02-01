using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Orchestrator.Cloudflare.Jobs
{
    public class Management : IManagementJobExtension
    {
        private readonly ILogger<Management> _logger;

        private string _thumbprint = string.Empty;

        public Management(ILogger<Management> logger)
        {
            _logger = logger;
        }

        public string ExtensionName => "Cloudflare";

        public JobResult ProcessJob(ManagementJobConfiguration jobConfiguration)
        {
            return new JobResult();
        }

        private JobResult PerformRemoval(ManagementJobConfiguration config)
        {
                return new JobResult();

        }

        private JobResult PerformAddition(ManagementJobConfiguration config,string thumpPrint)
        {
            return new JobResult();
        }
    }
}