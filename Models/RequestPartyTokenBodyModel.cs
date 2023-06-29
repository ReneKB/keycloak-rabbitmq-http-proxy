namespace keycloak_rabbitmq_http_proxy.Models
{
    public class RequestPartyTokenBodyModel
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string ticket { get; set; }
        public string grant_type { get; set; }
    }
}
