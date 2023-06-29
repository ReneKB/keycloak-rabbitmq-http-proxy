using keycloak_rabbitmq_http_proxy.Models;
using keycloak_rabbitmq_http_proxy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace keycloak_rabbitmq_http_proxy.Controllers
{
    [ApiController]
    [Route("/")]
    public class ProxyController : ControllerBase
    {
        private readonly ILogger<ProxyController> _logger;
        private readonly IKeycloakCommunicator _keycloakCommunicator;

        public ProxyController(ILogger<ProxyController> logger, 
            IKeycloakCommunicator keycloakCommunicator)
        {
            _logger = logger;
            _keycloakCommunicator = keycloakCommunicator;
        }

        [HttpPost]
        [Route("/user")]
        public async Task<String> PostUser([FromForm] UserPath path)
        {
            String response = "deny";
            _logger.LogInformation("User (Request): " + JsonConvert.SerializeObject(path));

            String[] roles = await _keycloakCommunicator.authenticate(path.username, path.password);
            if (roles != null)
                response = "allow " + string.Join(" ", roles);

            _logger.LogInformation("User (Response): " + response);
            return response;
        }

        [HttpPost]
        [Route("/vhost")]
        public async Task<String> PostVHost([FromForm] VHostPath path)
        {
            _logger.LogInformation("VHost (Request): " + JsonConvert.SerializeObject(path));
            string response = await _keycloakCommunicator.authorize(path.username, path.vhost, "vhost_access");
            _logger.LogInformation("VHost (Response): " + response);

            return response;
        }

        [HttpPost]
        [Route("/resource")]
        public async Task<String> PostResource([FromForm] ResourcePath path)
        {
            _logger.LogInformation("Resource (Request): " + JsonConvert.SerializeObject(path));
            string response = await _keycloakCommunicator.authorize(path.username, path.name, path.permission);
            _logger.LogInformation("Resource (Response): " + response);
            return response;
        }

        [HttpPost]
        [Route("/topic")]
        public async Task<String> PostTopic([FromForm] TopicPath path)
        {
            _logger.LogInformation("Topic: " + JsonConvert.SerializeObject(path));
            string response = await _keycloakCommunicator.authorize(path.username, path.name, path.permission);
            _logger.LogInformation("Response: " + response);
            return response;
        }
    }
}
