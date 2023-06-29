using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace keycloak_rabbitmq_http_proxy.Services
{
    public interface IKeycloakCommunicator
    {
        Task<String[]> authenticate(string username, string password);
        Task<String> authorize(string username, string resource, string scope);
    }
}
