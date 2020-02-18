using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
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
            string clientId = _configuration["WebhookClientId"];
            string clientSecret = _configuration["WebhookClientSecret"];
            string tenantId = _configuration["WebhookTenantId"];
            string subscriptionId = _configuration["WebhookSubscriptionId"];
            string resourceGroupName = _configuration["WebhookResourceGroup"];

            PullRequest result = await _codeRepo.ProcessPullRequest(payload, clientId, clientSecret, tenantId, subscriptionId, resourceGroupName);

            return (result != null) ? new OkResult() : new StatusCodeResult(500);
        }

        [HttpGet("Get")]
        public async Task<string> Get()
        {

            return "OK here!";
        }
    }
}