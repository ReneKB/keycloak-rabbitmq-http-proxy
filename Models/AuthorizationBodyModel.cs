namespace keycloak_rabbitmq_http_proxy.Models
{
    public class AuthorizationBodyModel
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string grant_type { get; set; }
        public string scope { get; set; }
    }
}
