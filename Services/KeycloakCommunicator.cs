using keycloak_rabbitmq_http_proxy.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace keycloak_rabbitmq_http_proxy.Services
{
    public class KeycloakCommunicator : IKeycloakCommunicator
    {
        private Dictionary<string, string> _cache = new Dictionary<string, string>();
        private Dictionary<string, string> _cache_refresh_token = new Dictionary<string, string>();
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeycloakCommunicator> _logger;

        public KeycloakCommunicator(IConfiguration configuration, ILogger<KeycloakCommunicator> logger)
        {
            this._configuration = configuration;
            this._logger = logger;
        }

        public async Task<String[]> authenticate(string username, string password)
        {
            string url = _configuration.GetValue<string>("Keycloak:TokenURL", "http://localhost:8080/auth/realms/REALM/protocol/openid-connect/token");

            // Authorization Body preparation
            AuthorizationBodyModel authorizationBodyModel = new AuthorizationBodyModel();
            authorizationBodyModel.scope = _configuration.GetValue<string>("Keycloak:Scope", "openid");
            authorizationBodyModel.client_id = _configuration.GetValue<string>("Keycloak:ClientID", null);
            authorizationBodyModel.client_secret = _configuration.GetValue<string>("Keycloak:ClientSecret", null);
            authorizationBodyModel.grant_type = _configuration.GetValue<string>("Keycloak:GrantType", null);
            authorizationBodyModel.password = password;
            authorizationBodyModel.username = username;

            // Prepare and send request
            string contentBody = JsonSerializer.Serialize(authorizationBodyModel);
            HttpContent httpContent = new FormUrlEncodedContent(JObject.FromObject(authorizationBodyModel).ToObject<Dictionary<string, string>>());
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            using var client = new HttpClient();
            _logger.LogDebug("Token [" + url + "]:" + contentBody);
            var response = await client.PostAsync(url, httpContent);
            
            // Handle response
            string responseString = await response.Content.ReadAsStringAsync();
            Dictionary<string, object> responseDictionary = this.ParseJson(responseString);

            if (!responseDictionary.ContainsKey("access_token"))
                return null;

            this._cache[username] = responseDictionary["access_token"].ToString();

            if (!responseDictionary.ContainsKey("refresh_token"))
                _logger.LogInformation("Could not obtain refresh token. This might lead to issues with re-auth. ");
            else
                this._cache_refresh_token[username] = responseDictionary["refresh_token"].ToString();

            // Add roles into 
            var decodedValue = new JwtSecurityTokenHandler().ReadJwtToken(responseDictionary["access_token"].ToString());

            List<string> tags = new List<string>();
            foreach (var claim in decodedValue.Claims)
            {
                if (claim.Type.ToString() == "resource_access")
                {
                    string jsonClaim = claim.Value.ToString();
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonClaim);

                    JObject attributesAsJObject = data;
                    Dictionary<string, object> values = attributesAsJObject.ToObject<Dictionary<string, object>>();
                    List<string> keys = values.Keys.ToList();

                    keys.ForEach(key =>
                    {
                        JArray values = data[key]["roles"];
                        tags.AddRange(values.Values().Select(x => x.ToString()).ToList());
                    });
                }
            }

            return tags.ToArray();
        }

        public async Task<bool> reauthenticate(string username)
        {
            string url = _configuration.GetValue<string>("Keycloak:TokenURL", "http://localhost:8080/auth/realms/REALM/protocol/openid-connect/token");

            // Check if a refresh-token exists. If not, reauth fails
            if (!_cache_refresh_token.ContainsKey(username))
                return false;

            String refreshToken = _cache_refresh_token[username];

            // Authorization Body preparation
            RefreshTokenBodyModel refreshTokenBody = new RefreshTokenBodyModel();
            refreshTokenBody.client_id = _configuration.GetValue<string>("Keycloak:ClientID", null);
            refreshTokenBody.grant_type = "refresh_token";
            refreshTokenBody.refresh_token = refreshToken;

            // Prepare and send request
            string contentBody = JsonSerializer.Serialize(refreshTokenBody);
            HttpContent httpContent = new FormUrlEncodedContent(JObject.FromObject(refreshTokenBody).ToObject<Dictionary<string, string>>());
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            using var client = new HttpClient();
            _logger.LogDebug("Refresh-Token [" + url + "]:" + contentBody);
            var response = await client.PostAsync(url, httpContent);
            
            // Handle response
            string responseString = await response.Content.ReadAsStringAsync();
            Dictionary<string, object> responseDictionary = this.ParseJson(responseString);

            if (!responseDictionary.ContainsKey("access_token"))
                return false;

            this._cache[username] = responseDictionary["access_token"].ToString();

            if (!responseDictionary.ContainsKey("refresh_token"))
                _logger.LogInformation("Could not obtain refresh token. This might lead to issues with re-auth. ");
            else
                this._cache_refresh_token[username] = responseDictionary["refresh_token"].ToString();

            return true;
        }

        async Task<Tuple<HttpResponseMessage, string, Dictionary<string, object>>> getUMATicket(String username, String resource, String scope, HttpClient client) {
            string permissionUrl = _configuration.GetValue<string>("Keycloak:PermissionURL", "http://localhost:8080/auth/realms/REALM/authz/protection/permission");
            
            var encodedJwtToken = this._cache[username];
            var decodedJwtToken = new JwtSecurityTokenHandler().ReadJwtToken(encodedJwtToken);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", encodedJwtToken);

            // Request body preparation
            List<ResourceBodyModel> requestObject = new List<ResourceBodyModel>();
            ResourceBodyModel resourceBody = new ResourceBodyModel();
            resourceBody.resource_scopes = new HashSet<string> { scope };
            resourceBody.resource_id = resource;
            requestObject.Add(resourceBody);

            // Prepare and send request (for receiving a UMA-Ticket)
            string umaTickethttpContentBody = JsonSerializer.Serialize(requestObject);
            HttpContent umaTickethttpContent = new StringContent(umaTickethttpContentBody, Encoding.UTF8, "application/json");
            _logger.LogDebug("UMA [" + permissionUrl + "]:" + umaTickethttpContentBody);
            HttpResponseMessage umaTicketHttpResponse = await client.PostAsync(permissionUrl, umaTickethttpContent);

            // Handle response (for receiving a UMA-Ticket)
            string umaTicketHttpResponseString = await umaTicketHttpResponse.Content.ReadAsStringAsync();
            return new Tuple<HttpResponseMessage, string, Dictionary<string, object>>(umaTicketHttpResponse, umaTicketHttpResponseString, this.ParseJson(umaTicketHttpResponseString));
        }

        public async Task<string> authorize(string username, string resource, string scope)
        {
            bool authenticationOnly = _configuration.GetValue<bool>("Keycloak:AuthenticationOnly", false);
            if (authenticationOnly)
                return "allow";

            String response = "deny";
            using HttpClient client = new HttpClient();
            string tokenUrl = _configuration.GetValue<string>("Keycloak:TokenURL", "http://localhost:8080/auth/realms/REALM/protocol/openid-connect/token");
            Tuple<HttpResponseMessage, string, Dictionary<string, object>> umaTicketResponse = await getUMATicket(username, resource, scope, client);

            if (umaTicketResponse.Item1.StatusCode != System.Net.HttpStatusCode.Created)
            {
                // Check if reauth is necessary -> do it and try again 
                bool success = await reauthenticate(username);
                if (success) {
                    umaTicketResponse = await getUMATicket(username, resource, scope, client);
                    response = await authorizeUMA(response, client, tokenUrl, umaTicketResponse);
                }
                _logger.LogInformation("Error on Authorization (Permission Endpoint). Response Code " + umaTicketResponse.Item1.StatusCode + ": " + umaTicketResponse.Item2);
            } else
            {
                // Request body preparation
                response = await authorizeUMA(response, client, tokenUrl, umaTicketResponse);
            }

            return response;
        }

        private async Task<string> authorizeUMA(string response, HttpClient client, string tokenUrl, Tuple<HttpResponseMessage, string, Dictionary<string, object>> umaTicketResponse)
        {
            RequestPartyTokenBodyModel requestPartyToken = new RequestPartyTokenBodyModel();
            requestPartyToken.client_id = _configuration.GetValue<string>("Keycloak:ClientID", null);
            requestPartyToken.client_secret = _configuration.GetValue<string>("Keycloak:ClientSecret", null);
            requestPartyToken.grant_type = "urn:ietf:params:oauth:grant-type:uma-ticket";
            requestPartyToken.ticket = (string)umaTicketResponse.Item3["ticket"];

            // Prepare and send request (for receiving a request party token)
            string requestPartyTokenString = JsonSerializer.Serialize(requestPartyToken);
            Dictionary<string, object> requestPartyTokenDictionary = this.ParseJson(requestPartyTokenString);
            List<KeyValuePair<string, string>> requestPartyKeyValuePairs = requestPartyTokenDictionary.ToList().Select(keyValuePair =>
                                                new KeyValuePair<string, string>(keyValuePair.Key, keyValuePair.Value.ToString())).ToList();

            HttpContent requestPartyTokenHttpContent = new FormUrlEncodedContent(requestPartyKeyValuePairs);
            _logger.LogDebug("Requesty Party Token [" + tokenUrl + "]: " + requestPartyTokenString);
            HttpResponseMessage requestPartyTokenHttpResponse = await client.PostAsync(tokenUrl, requestPartyTokenHttpContent);

            // Handle response (for receiving a request party token)
            if (requestPartyTokenHttpResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                response = "allow";
            }
            else
            {
                string requestPartyTokenHttpResponseString = await requestPartyTokenHttpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Error on Authorization (Ticket Endpoint). Response Code: " + requestPartyTokenHttpResponse.StatusCode + ": " + requestPartyTokenHttpResponseString);
            }

            return response;
        }

        private Dictionary<string, object> ParseJson(string json)
        {
            var obj = JObject.Parse(json);
            var dict = new Dictionary<string, object>();

            foreach (var property in obj)
            {
                var name = property.Key;
                var value = property.Value;

                if (value is JArray)
                {
                    dict.Add(name, value.ToArray());
                }
                else if (value is JValue)
                {
                    dict.Add(name, value.ToString());
                }
                else
                {
                    throw new NotSupportedException("Invalid JSON token type.");
                }
            }

            return dict;
        }
    }
}
