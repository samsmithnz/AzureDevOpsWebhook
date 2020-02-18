using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;

using Microsoft.TeamFoundation.Common;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System.Text;
using WebhookReceiver.Service.Models;
using WebhookReceiver.Service.Repos;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;
using Microsoft.AspNetCore.Identity;

namespace WebhookReceiver.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebHooksController : ControllerBase
    {
        private readonly ICodeRepo _codeRepo;
        private readonly IConfiguration Configuration;

        public WebHooksController(ICodeRepo codeRepo, IConfiguration configuration)
        {
            _codeRepo = codeRepo;
            Configuration = configuration;
        }

        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JObject payload)
        {
            string clientId = Configuration["WebhookClientId"];
            string clientSecret = Configuration["WebhookClientSecret"];
            string tenantId = Configuration["WebhookTenantId"];
            string subscriptionId = Configuration["WebhookSubscriptionId"];
            string resourceGroupName = Configuration["WebhookResourceGroup"];
          
            PullRequest result = await _codeRepo.ProcessPullRequest(payload, clientId, clientSecret, tenantId, subscriptionId, resourceGroupName);

            return (result != null) ? new OkResult() : new StatusCodeResult(500);
        }
    }
}