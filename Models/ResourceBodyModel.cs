using System.Collections.Generic;

namespace keycloak_rabbitmq_http_proxy.Models
{
    public class ResourceBodyModel
    { 
        public string resource_id { get; set; }
        public ISet<string> resource_scopes { get; set; }
    }
}
