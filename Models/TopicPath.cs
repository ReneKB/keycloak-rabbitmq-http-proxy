namespace keycloak_rabbitmq_http_proxy.Models
{
    public class TopicPath
    {
        public string username { get; set; }
        public string vhost { get; set; }
        public string resource { get; set; }
        public string name { get; set; }
        public string permission { get; set; }
        public string routing_key { get; set; }
        public string tags { get; set; }
        public TopicVariableMapPath variable_map { get; set; }
    }
}
