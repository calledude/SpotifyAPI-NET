using EmbedIO.WebApi;
using Newtonsoft.Json;
using SpotifyAPI.Web.Auth.Controllers;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyAPI.Web.Auth
{
    public class AuthorizationCodeAuth : SpotifyAuthServer<AuthorizationCodeResponse>
    {
        public string SecretId { get; set; }

        public ProxyConfig ProxyConfig { get; set; }

        public AuthorizationCodeAuth() : base("code", "AuthorizationCodeAuth", null, null)
        {
        }

        private AuthorizationCodeAuth(string redirectUri, string serverUri, Scope scope = Scope.None, string state = "")
            : base("code", "AuthorizationCodeAuth", redirectUri, serverUri, scope, state)
        {
        }

        public AuthorizationCodeAuth(string clientId, string redirectUri, string serverUri, Scope scope = Scope.None, string state = "")
            : this(redirectUri, serverUri, scope, state)
        {
            ClientId = clientId;
        }

        public AuthorizationCodeAuth(string clientId, string secretId, string redirectUri, string serverUri, Scope scope = Scope.None, string state = "")
            : this(redirectUri, serverUri, scope, state)
        {
            ClientId = clientId;
            SecretId = secretId;
        }

        private bool ShouldRegisterNewApp()
        {
            return string.IsNullOrEmpty(SecretId) || string.IsNullOrEmpty(ClientId);
        }

        public override string GetUri()
        {
            return ShouldRegisterNewApp() ? $"{RedirectUri}/start.html#{State}" : base.GetUri();
        }

        protected override void AdaptModule(WebApiModule webApiModule)
        {
            webApiModule.RegisterController<AuthorizationCodeAuthController>();
        }

        private string GetAuthHeader() => $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(ClientId + ":" + SecretId))}";

        public async Task<Token> RefreshToken(string refreshToken)
        {
            List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            };

            var t = await GetToken(args);
            t.RefreshToken = t.RefreshToken ?? refreshToken;
            return t;
        }

        internal async Task<Token> ExchangeCode(string code)
        {
            List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", RedirectUri)
            };

            return await GetToken(args);
        }

        private async Task<Token> GetToken(IEnumerable<KeyValuePair<string, string>> args)
        {
            using (HttpClientHandler handler = ProxyConfig.CreateClientHandler(ProxyConfig))
            using (HttpClient client = new HttpClient(handler))
            using (HttpContent content = new FormUrlEncodedContent(args))
            {
                client.DefaultRequestHeaders.Add("Authorization", GetAuthHeader());
                HttpResponseMessage resp = await client.PostAsync("https://accounts.spotify.com/api/token", content);
                string msg = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Token>(msg);
            }
        }
    }
}
