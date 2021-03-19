using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using WebhookReceiver.Service.Models;
using WebhookReceiver.Service.Repos;

namespace WebhookReceiver.Tests
{
    [TestClass]
    public class WebHookControllerTests
    {

        private string ClientId;
        private string ClientSecret;
        private string TenantId;
        private string SubscriptionId;
        private string ResourceGroupName;
        private string KeyVaultQueueName;
        private string StorageConnectionString;

        [TestInitialize]
        public void InitializeTests()
        {
            //Key vault access
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.json")
               .AddUserSecrets<WebHookControllerTests>();
            IConfigurationRoot Configuration = configBuilder.Build();

            string azureKeyVaultURL = Configuration["AppSettings:KeyVaultURL"];
            string keyVaultClientId = Configuration["AppSettings:ClientId"];
            string keyVaultClientSecret = Configuration["AppSettings:ClientSecret"];
            configBuilder.AddAzureKeyVault(azureKeyVaultURL, keyVaultClientId, keyVaultClientSecret);
            Configuration = configBuilder.Build();

            //Setup the repo
            ClientId = Configuration["AppSettings:ClientId"];
            ClientSecret = Configuration["WebhookClientSecret"];
            TenantId = Configuration["WebhookTenantId"];
            SubscriptionId = Configuration["WebhookSubscriptionId"];
            ResourceGroupName = Configuration["AppSettings:WebhookResourceGroup"];
            KeyVaultQueueName = Configuration["AppSettings:KeyVaultQueue"];
            StorageConnectionString = Configuration["AppSettings:StorageConnectionString"];
        }

        [TestMethod]
        public async Task ProcessingSamplePayloadTest()
        {
            //Arrange
            JObject payload = ReadJSON(@"/Sample/sample.json");
            CodeRepo code = new CodeRepo();

            //Act
            PullRequest pr = await code.ProcessPullRequest(payload,
                ClientId, ClientSecret,
                TenantId, SubscriptionId, ResourceGroupName,
                KeyVaultQueueName, StorageConnectionString);

            //Assert
            Assert.IsTrue(pr != null);
            Assert.AreEqual(1, pr.Id);
            Assert.AreEqual("completed", pr.Status);
            Assert.AreEqual("my first pull request", pr.Title);
        }

        [TestMethod]
        public async Task ProcessingSample2PayloadTest()
        {
            //Arrange
            JObject payload = ReadJSON(@"/Sample/sample2.json");
            CodeRepo code = new CodeRepo();

            //Act
            PullRequest pr = await code.ProcessPullRequest(payload,
                ClientId, ClientSecret,
                TenantId, SubscriptionId, ResourceGroupName,
                KeyVaultQueueName, StorageConnectionString);

            //Assert
            Assert.IsTrue(pr != null);
            Assert.AreEqual(549, pr.Id);
            Assert.AreEqual("completed", pr.Status);
            Assert.AreEqual("Upgraded to Dapper. Testing performance is terrible for some reason", pr.Title);
        }

        [TestMethod]
        public async Task ProcessingEmptyPayloadTest()
        {
            //Arrange
            JObject payload = ReadJSON(@"/Sample/emptySample.json");
            CodeRepo code = new();

            //Act
            try
            {
                PullRequest pr = await code.ProcessPullRequest(payload,
                ClientId, ClientSecret,
                TenantId, SubscriptionId, ResourceGroupName,
                KeyVaultQueueName, StorageConnectionString);
            }
            catch (Exception ex)
            {
                //Assert
                Assert.IsTrue(ex.ToString() != "");
            }
        }
        private static JObject ReadJSON(string fileName)
        {
            JObject payload;
            // read JSON directly from a file
            using (StreamReader file = System.IO.File.OpenText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + fileName))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    payload = (JObject)JToken.ReadFrom(reader);
                }
            }

            return payload;
        }
    }
}
