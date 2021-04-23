using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
                string keyVaultQueueName, string keyVaultSecretsQueueName, string storageConnectionString,
                string goDaddyAPIKey, string goDaddyAPISecret)
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
                throw new Exception("Misconfiguration: storage policies queue name is null");
            }
            else if (string.IsNullOrEmpty(keyVaultSecretsQueueName) == true)
            {
                throw new Exception("Misconfiguration: storage secrets queue name is null");
            }
            else if (string.IsNullOrEmpty(storageConnectionString) == true)
            {
                throw new Exception("Misconfiguration: storage connection string is null");
            }
            else if (string.IsNullOrEmpty(goDaddyAPIKey) == true)
            {
                throw new Exception("Misconfiguration: godaddy API key is null");
            }
            else if (string.IsNullOrEmpty(goDaddyAPISecret) == true)
            {
                throw new Exception("Misconfiguration: godaddy API secret is null");
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
                //Clean up the key vault
                AzureCredentials creds = new AzureCredentialsFactory().FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
                fluent.IAzure azure = fluent.Azure.Authenticate(creds).WithSubscription(subscriptionId);

                RestClient _restClient = RestClient
                   .Configure()
                   .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                   .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                   .WithCredentials(creds)
                   .Build();

                //Clean up the DNS settings in GoDaddy
                string prId = pr.Id.ToString();
                string web1DNS = $"pr{prId}";
                string web2DNS = $"pr{prId}2";
                string fdDNS = $"pr{prId}fd";

                //Delete the items from GoDaddy
                await CleanUpGoDaddy(goDaddyAPIKey, goDaddyAPISecret, web1DNS, web2DNS, fdDNS);

                //process web apps to remove identities
                ResourceManagementClient resourceManagementClient = new ResourceManagementClient(_restClient)
                {
                    SubscriptionId = subscriptionId
                };

                //Look for web apps in the resource group
                bool resourceGroupExists = await azure.ResourceGroups.ContainAsync(resourceGroupName);
                if (resourceGroupExists == true)
                {
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
                    //Add the PR name to a queue
                    InsertMessage(storageConnectionString, keyVaultSecretsQueueName, "PR" + pr.Id.ToString());

                    //Delete the resource group
                    await azure.ResourceGroups.DeleteByNameAsync(resourceGroupName);
                }
            }

            return pr;
        }

        private static void InsertMessage(string connectionString, string queueName, string message)
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

        public async static Task<bool> CleanUpGoDaddy(string goDaddyKey, string goDaddySecret, string web1DNS, string web2DNS, string fdDNS)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri("https://api.godaddy.com/")
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("sso-key", goDaddyKey + ":" + goDaddySecret);

            string godaddy_domain = "samlearnsazure.com";
            string godaddy_type = "CNAME";

            Uri url1 = new Uri($"v1/domains/{godaddy_domain}/records/{godaddy_type}/{web1DNS}", UriKind.Relative);
            Uri url2 = new Uri($"v1/domains/{godaddy_domain}/records/{godaddy_type}/{web2DNS}", UriKind.Relative);
            Uri url3 = new Uri($"v1/domains/{godaddy_domain}/records/{godaddy_type}/{fdDNS}", UriKind.Relative);

            HttpResponseMessage response1 = await client.DeleteAsync(url1);
            HttpResponseMessage response2 = await client.DeleteAsync(url2);
            HttpResponseMessage response3 = await client.DeleteAsync(url3);

            if (response1.IsSuccessStatusCode == true & response2.IsSuccessStatusCode == true & response3.IsSuccessStatusCode == true)
            {
                return true;
            }
            else if (response1.StatusCode == System.Net.HttpStatusCode.NotFound | response2.StatusCode == System.Net.HttpStatusCode.NotFound | response3.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return true;
            }
            else
            {
                if (response1.IsSuccessStatusCode == false)
                {
                    Debug.WriteLine("response1 failed");
                    Debug.WriteLine(response1.Content.ToString());
                }
                if (response2.IsSuccessStatusCode == false)
                {
                    Debug.WriteLine("response2 failed");
                    Debug.WriteLine(response2.Content.ToString());
                }
                if (response3.IsSuccessStatusCode == false)
                {
                    Debug.WriteLine("response3 failed");
                    Debug.WriteLine(response3.Content.ToString());
                }
                return false;
            }
        }
    }

    public interface ICodeRepo
    {
        Task<PullRequest> ProcessPullRequest(JObject payload,
                string clientId, string clientSecret,
                string tenantId, string subscriptionId, string resourceGroupName,
                string keyVaultQueueName, string keyVaultSecretsQueueName, string storageConnectionString,
                string goDaddyAPIKey, string goDaddyAPISecret);
    }

}
