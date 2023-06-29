# keycloak-rabbitmq-http-proxy

A straightforward easy-to-use proxy-service for RabbitMQ authentification and authorisation written in ASP.NET Core (C#) in combination with Keycloak. Allows flexible changes for your own use case. 

## Configuration in RabbitMQ
On RabbitMQ side ensure that the plugin ```rabbitmq_auth_backend_http``` is activated beforehand. Inside the ```rabbitmq.conf``` the following block should be added in accordance with the proper values for the variables ```<<proxy-url>>``` and ```<<proxy-port>>``` (default http://localhost:5000/*):

```
auth_backends.1 = http

auth_http.http_method   = post
auth_http.user_path     = http://<<proxy-url>>:<<proxy-port>>/user
auth_http.vhost_path    = http://<<proxy-url>>:<<proxy-port>>/vhost
auth_http.resource_path = http://<<proxy-url>>:<<proxy-port>>/resource
auth_http.topic_path    = http://<<proxy-url>>:<<proxy-port>>/topic
```

The HTTP specification according RabbitMQ can be taken from the documentation: https://github.com/rabbitmq/rabbitmq-auth-backend-http and https://www.rabbitmq.com/access-control.html

## Configuration in Keycloak
Both **Authentication and Authorisation** aspects are handled by the a Keycloak instance. The ```keycloak-rabbitmq-http-proxy``` has been developed against a *Client with the confidential access type*. Due the activated functionality *Authorization enabled* resources, scopes and policies can be configured in the client internally. The *Authorization system* is explained by the official documentation: https://www.keycloak.org/docs/latest/authorization_services/index.html

For the use of the proxy it's **recommended to use the following Client-settings** for the securing client:
* **Client Protocol:** openid-connect
* **Access Type:** confidential
* **Direct Access Grants Enabled:** On
* **Service Accounts Enabled:** On
* **OAuth 2.0 Device Authorization Grant Enabled:** On
* **Authorization Enabled:** On
* A scope (e.g ```roles```) returning all the user roles should be set. It's recommended to have it inside the client.

After activating the authorization, **the following Authorization Scopes are mandatory** to be set:
* **write** Writing access to a resource
* **read** Reading access to a resource
* **configure** Managing access to a resource
* **vhost_access** Different VHosts are supported

The VHost functionality also allows the proxy to secure MQTT topics (with the ```rabbitmq_mqtt``` plugin).

## Configuration of the proxy itself

```appsettings.json``` contains runtime variables of the proxy which differs from every Keycloak instance and project:
* **Keycloak.TokenUrl** The URL of the OpenID-Connect Token Endpoint (e. g. http://localhost:8080/auth/realms/master/protocol/openid-connect/token) 
* **Keycloak.PermissionURL** The URL of the Authz Permission Endpoint (e. g. http://localhost:8080/auth/realms/master/authz/protection/permission) 
* **Keycloak.ClientID** The Client ID of concern
* **Keycloak.ClientSecret** The corresponding client secret 
* **Keycloak.Scope** The scope of relevance (e. g. ```roles```)
* **Keycloak.GrantType** The grant type of the client (most cases ```password```)
* **Keycloak.AuthenticationOnly** Auto allow VHost/Topic/Resource endpoints of the proxy. Only the Authentication-Endpoint is running the according logic. 

The proxy is following the standardized way of the ```rabbitmq_auth_backend_http```. Configurations towards the RabbitMQ instance are not required.  

