using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using WebhookReceiver.Service.Models;
using WebhookReceiver.Service.Repos;

namespace WebhookReceiver.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebHooksController : ControllerBase
    {
        private readonly ICodeRepo _codeRepo;
        private readonly IConfiguration _configuration;

        public WebHooksController(ICodeRepo codeRepo, IConfiguration configuration)
        {
            _codeRepo = codeRepo;
            _configuration = configuration;
        }

        // POST api/values
        [HttpPost("Post")]
        public async Task<IActionResult> Post(JObject payload)
        {
            string clientId = _configuration["AppSettings:ClientId"];
            string clientSecret = _configuration["AppSettings:ClientSecret"];
            //string clientId = _configuration["WebhookClientId"];
            //string clientSecret = _configuration["WebhookClientSecret"];
            string tenantId = _configuration["WebhookTenantId"];
            string subscriptionId = _configuration["WebhookSubscriptionId"];
            string resourceGroupName = _configuration["AppSettings:WebhookResourceGroup"];
            string keyVaultQueueName = _configuration["AppSettings:KeyVaultQueue"];
            string keyVaultSecretsQueueName = _configuration["AppSettings:KeyVaultSecretsQueue"];
            string storageConnectionString = _configuration["AppSettings:StorageConnectionString"];
            string goDaddyKey = _configuration["GoDaddyAPIKey"];
            string goDaddySecret = _configuration["GoDaddyAPISecret"];

            //Add identities to queue, if they don't exist.
            PullRequest result = await _codeRepo.ProcessPullRequest(payload,
                clientId, clientSecret, tenantId, subscriptionId, resourceGroupName,
                keyVaultQueueName, keyVaultSecretsQueueName, storageConnectionString,
                goDaddyKey, goDaddySecret);

            return (result != null) ? new OkResult() : new StatusCodeResult(500);
        }

        [HttpGet("Get")]
        public async Task<string> Get()
        {
            string message = "this works! ";  
            //string clientId = _configuration["AppSettings:ClientId"];
            //string clientSecret = _configuration["AppSettings:ClientSecret"];
            //string clientId2 = _configuration["WebhookClientId"];
            //string clientSecret2 = _configuration["WebhookClientSecret"];
            string tenantId = _configuration["WebhookTenantId"];
            string subscriptionId = _configuration["WebhookSubscriptionId"];
            //string goDaddyKey = _configuration["GoDaddyAPIKey"];
            //string goDaddySecret = _configuration["GoDaddyAPISecret"];

            //for (int i = 540; i <= 552; i++)
            //{
            //    string web1 = "pr" + i.ToString();
            //    string web2 = "pr" + i.ToString() + "2";
            //    string webfd = "pr" + i.ToString() + "fd";
            //    bool result = await CodeRepo.CleanUpGoDaddy(goDaddyKey, goDaddySecret, web1, web2, webfd);
            //    message += "processing " + web1 + "," + web2 + "," + webfd + Environment.NewLine;
            //    if (result == false)
            //    {
            //        message += "failed " + web1 + "," + web2 + "," + webfd;
            //        break;
            //    }
            //}

            message += Environment.NewLine;
            //message += "clientId: " + clientId + Environment.NewLine;
            //message += "clientSecret: " + clientSecret + Environment.NewLine;
            //message += "clientId2: " + clientId2 + Environment.NewLine;
            //message += "clientSecret2: " + clientSecret2 + Environment.NewLine;
            message += "tenantId: " + tenantId + Environment.NewLine;
            message += "subscriptionId: " + subscriptionId + Environment.NewLine;

            return message;
        }
    }
}