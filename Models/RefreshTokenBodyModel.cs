namespace keycloak_rabbitmq_http_proxy.Models
{
    public class RefreshTokenBodyModel
    {
        public string client_id { get; set; }
        public string refresh_token { get; set; }
        public string grant_type  { get; set; }
    }
}
