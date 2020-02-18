
using Hyak.Common;
using Microsoft.Azure;
using Microsoft.Azure.Management.ContainerRegistry.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ManagementGroups;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.VisualStudio.Services.Commerce;
//using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
//using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WebhookReceiver.Service.Models;

namespace WebhookReceiver.Service.Repos
{
    public class CodeRepo : ICodeRepo
    {
        public async Task<PullRequest> ProcessPullRequest(JObject payload, string clientId, string clientSecret, string tenantId, string subscriptionId, string resourceGroupName)
        {

            //Get pull request details
            PullRequest pr = new PullRequest
            {
                Id = payload["resource"]["pullRequestId"] == null ? -1 : Convert.ToInt32(payload["resource"]["pullRequestId"].ToString()),
                Status = payload["resource"]["status"]?.ToString(),
                Title = payload["resource"]["title"]?.ToString()
            };

            //Delete the resource group
            resourceGroupName = resourceGroupName.Replace("__###__", "PR" + pr.Id.ToString());

            if (pr != null && (pr.Status == "completed" || pr.Status == "abandoned"))
            {
                var creds = new AzureCredentialsFactory().FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
                var azure = Azure.Authenticate(creds).WithSubscription(subscriptionId);

                bool rgExists = await azure.ResourceGroups.ContainAsync(resourceGroupName);
                if (rgExists == true)
                {
                    await azure.ResourceGroups.DeleteByNameAsync(resourceGroupName);
                }
            }

            return pr;
        }
    }

    public interface ICodeRepo
    {
        Task<PullRequest> ProcessPullRequest(JObject payload, string clientId, string clientSecret, string tenantId, string subscriptionId, string resourceGroupName);
    }

}
