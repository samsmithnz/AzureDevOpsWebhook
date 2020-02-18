using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WebhookReceiver.Service;
using WebhookReceiver.Service.Models;
using WebhookReceiver.Service.Repos;

namespace WebhookReceiver.Tests
{
    [TestClass]
    public class WebHookControllerTests
    {
        //[TestMethod]
        //public async Task PostTest()
        //{
        //    //Arrange
        //    IConfigurationBuilder config = new ConfigurationBuilder()
        //       .SetBasePath(AppContext.BaseDirectory)
        //       .AddJsonFile("appsettings.json");
        //    IConfiguration configuration = config.Build();
        //    TestServer _server = new TestServer(WebHost.CreateDefaultBuilder()
        //        .UseConfiguration(configuration)
        //        .UseStartup<Startup>());
        //    HttpClient _client = _server.CreateClient();
        //    string username = "";
        //    string password = configuration["AppSettings:AzureDevOpsToken"];
        //    string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
        //    _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Basic " + svcCredentials);
        //    _client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
        //    _client.BaseAddress = new Uri(@"https://localhost:44350/");
        //    JObject payload;

        //    //Act

        //    // read JSON directly from a file
        //    using (StreamReader file = System.IO.File.OpenText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\Sample\sample.json"))
        //    {
        //        using (JsonTextReader reader = new JsonTextReader(file))
        //        {
        //            payload = (JObject)JToken.ReadFrom(reader);
        //        }
        //    }

        //    StringContent content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        //    HttpResponseMessage response = await _client.PostAsync(@"api/webhooks", content);

        //    //Assert
        //    Assert.IsTrue(response != null);

        //}

        [TestMethod]
        public async Task ProcessingSamplePayloadTest()
        {
            //Arrange
            JObject payload;
            // read JSON directly from a file
            using (StreamReader file = System.IO.File.OpenText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\Sample\sample.json"))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    payload = (JObject)JToken.ReadFrom(reader);
                }
            }
            //Key vault access
            IConfigurationBuilder config = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.json");
            IConfigurationRoot Configuration = config.Build();

            string azureKeyVaultURL = Configuration["AppSettings:KeyVaultURL"];
            string keyVaultClientId = Configuration["AppSettings:ClientId"];
            string keyVaultClientSecret = Configuration["AppSettings:ClientSecret"];
            config.AddAzureKeyVault(azureKeyVaultURL, keyVaultClientId, keyVaultClientSecret);
            Configuration = config.Build();
            
            //Setup the repo
            CodeRepo code = new CodeRepo();
            string clientId = Configuration["WebhookClientId"];
            string clientSecret = Configuration["WebhookClientSecret"];
            string tenantId = Configuration["WebhookTenantId"];
            string subscriptionId = Configuration["WebhookSubscriptionId"];
            string resourceGroupName = Configuration["WebhookResourceGroup"];

            //Act
            PullRequest pr = await code.ProcessPullRequest(payload, clientId, clientSecret, tenantId, subscriptionId, resourceGroupName);

            //Assert
            Assert.IsTrue(pr != null);
            Assert.IsTrue(pr.Id == 1);
            Assert.IsTrue(pr.Status == "completed");
            Assert.IsTrue(pr.Title == "my first pull request");
        }
    }
}
