using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebhookReceiver.Service.Models;
using fluent = Microsoft.Azure.Management.Fluent;
using queue = Azure.Storage.Queues;

namespace WebhookReceiver.Service.Repos
{
    public class CodeRepo : ICodeRepo
    {
        public async Task<PullRequest> ProcessPullRequest(JObject payload,
                string clientId, string clientSecret,
                string tenantId, string subscriptionId, string resourceGroupName,
                string keyVaultQueueName, string storageConnectionString)
        {
            //Validate the payload
            if (payload["resource"] == null)
            {
                throw new Exception("Payload resource is null");
            }
            else if (payload["resource"]["status"] == null)
            {
                throw new Exception("Payload resource status is null");
            }
            else if (payload["resource"]["title"] == null)
            {
                throw new Exception("Payload title is null");
            }
            else if (payload["resource"]["pullRequestId"] == null)
            {
                throw new Exception("Payload pullRequestId is null");
            }
            else if (string.IsNullOrEmpty(clientId) == true)
            {
                throw new Exception("Misconfiguration: client id is null");
            }
            else if (string.IsNullOrEmpty(clientSecret) == true)
            {
                throw new Exception("Misconfiguration: client secret is null");
            }
            else if (string.IsNullOrEmpty(tenantId) == true)
            {
                throw new Exception("Misconfiguration: tenant id is null");
            }
            else if (string.IsNullOrEmpty(subscriptionId) == true)
            {
                throw new Exception("Misconfiguration: subscription id is null");
            }
            else if (string.IsNullOrEmpty(resourceGroupName) == true)
            {
                throw new Exception("Misconfiguration: resource group is null");
            }
            else if (string.IsNullOrEmpty(keyVaultQueueName) == true)
            {
                throw new Exception("Misconfiguration: storage queue name is null");
            }
            else if (string.IsNullOrEmpty(storageConnectionString) == true)
            {
                throw new Exception("Misconfiguration: storage connection string is null");
            }

            //Get pull request details
            PullRequest pr = new PullRequest
            {
                Id = payload["resource"]["pullRequestId"] == null ? -1 : Convert.ToInt32(payload["resource"]["pullRequestId"].ToString()),
                Status = payload["resource"]["status"]?.ToString(),
                Title = payload["resource"]["title"]?.ToString()
            };
            resourceGroupName = resourceGroupName.Replace("__###__", "PR" + pr.Id.ToString());

            //If the PR is completed or abandoned, clean up the secrets and permissions from key vault and then delete the resource group/resources
            if (pr != null && (pr.Status == "completed" || pr.Status == "abandoned"))
            {
                AzureCredentials creds = new AzureCredentialsFactory().FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
                fluent.IAzure azure = fluent.Azure.Authenticate(creds).WithSubscription(subscriptionId);

                RestClient _restClient = RestClient
                   .Configure()
                   .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                   .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                   .WithCredentials(creds)
                   .Build();

                ResourceManagementClient resourceManagementClient = new ResourceManagementClient(_restClient)
                {
                    SubscriptionId = subscriptionId
                };
          
                //Look for web apps in the resource group
                Microsoft.Rest.Azure.IPage<GenericResourceInner> resources = await resourceManagementClient.Resources.ListByResourceGroupAsync(resourceGroupName);
                List<string> identities = new List<string>();
                foreach (GenericResourceInner item in resources)
                {
                    if (item.Type == "Microsoft.Web/sites" | item.Type == "Microsoft.Web/sites/slots")
                    {
                        //Get identities of the web apps and their slots
                        identities.Add(item.Identity.PrincipalId.ToString());
                    }
                }

                //insert the identities into a storage queue
                foreach (string identity in identities)
                {
                    InsertMessage(storageConnectionString, keyVaultQueueName, identity);
                }

                //Delete the resource group
                bool rgExists = await azure.ResourceGroups.ContainAsync(resourceGroupName);
                if (rgExists == true)
                {
                    await azure.ResourceGroups.DeleteByNameAsync(resourceGroupName);
                }
            }

            return pr;
        }

        private void InsertMessage(string connectionString, string queueName, string message)
        {
            // Instantiate a QueueClient which will be used to create and manipulate the queue
            queue.QueueClient queueClient = new queue.QueueClient(connectionString, queueName);

            // Create the queue if it doesn't already exist
            queueClient.CreateIfNotExists();

            if (queueClient.Exists())
            {
                // Send a message to the queue
                queueClient.SendMessage(message);
            }

            Console.WriteLine($"Inserted: {message}");
        }
    }

    public interface ICodeRepo
    {
        Task<PullRequest> ProcessPullRequest(JObject payload,
                string clientId, string clientSecret,
                string tenantId, string subscriptionId, string resourceGroupName,
                string keyVaultQueueName, string storageConnectionString);
    }

}
