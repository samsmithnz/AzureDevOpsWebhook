using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using WebhookReceiver.Service.Models;

namespace WebhookReceiver.Service.Repos
{
    public class CodeRepo : ICodeRepo
    {
        public async Task<PullRequest> ProcessPullRequest(JObject payload, string clientId, string clientSecret, string tenantId, string subscriptionId, string resourceGroupName)
        {
            //Validate the payload
            if (payload["resource"] == null)
            {
                return null;
            }
            else if (payload["resource"]["status"] == null)
            {
                return null;
            }
            else if (payload["resource"]["title"] == null)
            {
                return null;
            }    
            else if (string.IsNullOrEmpty(clientId) == true || string.IsNullOrEmpty(clientSecret) == true || string.IsNullOrEmpty(tenantId) == true || string.IsNullOrEmpty(subscriptionId) == true || string.IsNullOrEmpty(resourceGroupName) == true)
            {
                return null;
            }

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
