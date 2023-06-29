namespace keycloak_rabbitmq_http_proxy.Models
{
    public class ResourcePath
    {
        public string username { get; set; } 
        public string vhost { get; set; }
        public string resource { get; set; }
        public string name { get; set; }
        public string permission { get; set; }
        public string tags { get; set; }
    }
}
