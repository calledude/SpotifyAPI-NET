﻿using EmbedIO.WebApi;
using Newtonsoft.Json;
using SpotifyAPI.Web.Auth.Controllers;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SpotifyAPI.Web.Auth
{
    /// <summary>
    /// <para>
    /// A version of <see cref="AuthorizationCodeAuth"/> that does not store your client secret, client ID or redirect URI, enforcing a secure authorization flow. Requires an exchange server that will return the authorization code to its callback server via GET request.
    /// </para>
    /// <para>
    /// It's recommended that you use <see cref="TokenSwapWebAPIFactory"/> if you would like to use the TokenSwap method.
    /// </para>
    /// </summary>
    public class TokenSwapAuth : SpotifyAuthServer<AuthorizationCodeResponse>
    {
        private readonly string _exchangeServerUri;
        private Timer _accessTokenExpireTimer;
        private string _htmlResponse = "<script>window.close();</script>";

        /// <summary>
        /// The HTML to respond with when the callback server (serverUri) is reached. The default value will close the window on arrival.
        /// </summary>
        public string HtmlResponse
        {
            get => _htmlResponse;
            set
            {
                if (!string.IsNullOrEmpty(value))
                    _htmlResponse = value;
            }
        }

        /// <summary>
        /// If true, will time how long it takes for access to expire. On expiry, the <see cref="OnAccessTokenExpired"/> event fires.
        /// </summary>
        public bool TimeAccessExpiry { get; set; }

        public ProxyConfig ProxyConfig { get; set; }

        /// <summary>
        /// The maximum amount of times to retry getting a token.
        /// <para/>
        /// A token get is attempted every time you <see cref="RefreshAuthAsync(string)"/> and <see cref="ExchangeCodeAsync(string)"/>.
        /// </summary>
        public int MaxGetTokenRetries { get; set; } = 10;

        /// <summary>
        /// When Spotify authorization has expired. Will only trigger if <see cref="TimeAccessExpiry"/> is true.
        /// </summary>
        public event EventHandler OnAccessTokenExpired;

        /// <param name="exchangeServerUri">The URI to an exchange server that will perform the key exchange.</param>
        /// <param name="serverUri">The URI to host the server at that your exchange server should return the authorization code to by GET request. (e.g. http://localhost:4002)</param>
        /// <param name="scope"></param>
        /// <param name="htmlResponse">The HTML to respond with when the callback server (serverUri) is reached. The default value will close the window on arrival.</param>
        /// <param name="state">Stating none will randomly generate a state parameter.</param>
        public TokenSwapAuth(string exchangeServerUri, string serverUri, Scope scope = Scope.None, string htmlResponse = "",
            string state = "") : base("code", "", "", serverUri, scope, state)
        {
            HtmlResponse = htmlResponse;
            _exchangeServerUri = exchangeServerUri;
        }

        protected override void AdaptModule(WebApiModule webApiModule)
            => webApiModule.RegisterController<TokenSwapAuthController>();

        public override string GetUri()
        {
            var builder = new StringBuilder(_exchangeServerUri + "/authorize");
            builder.Append("?");
            builder.Append("response_type=code");
            builder.Append("&state=" + State);
            builder.Append("&scope=" + Scope.GetStringAttribute(" "));
            builder.Append("&show_dialog=" + ShowDialog);
            return Uri.EscapeUriString(builder.ToString());
        }

        /// <summary>
        /// Creates a HTTP request to obtain a token object.
        /// Parameter grantType can only be "refresh_token" or "authorization_code". authorizationCode and refreshToken are not mandatory, but at least one must be provided for your desired grant_type request otherwise an invalid response will be given and an exception is likely to be thrown.
        /// <para>
        /// Will re-attempt on error, on null or on no access token <see cref="MaxGetTokenRetries"/> times before finally returning null.
        /// </para>
        /// </summary>
        /// <param name="grantType">Can only be "refresh_token" or "authorization_code".</param>
        /// <param name="endpoint">What action to execute. E.g. '/refresh'</param>
        /// <param name="authorizationCode">This needs to be defined if "grantType" is "authorization_code".</param>
        /// <param name="refreshToken">This needs to be defined if "grantType" is "refresh_token".</param>
        /// <param name="currentRetries">Does not need to be defined. Used internally for retry attempt recursion.</param>
        /// <returns>Attempts to return a full <see cref="Token"/>, but after retry attempts, may return a <see cref="Token"/> with no <see cref="Token.AccessToken"/>, or null.</returns>
        private async Task<Token> GetToken(string grantType, string endpoint, string authorizationCode = "", string refreshToken = "",
            int currentRetries = 0)
        {
            var parameters = new Dictionary<string, string>()
            {
                {"grant_type", grantType}
            };

            if (!string.IsNullOrEmpty(authorizationCode))
                parameters.Add("code", authorizationCode);
            else if (!string.IsNullOrEmpty(refreshToken))
                parameters.Add("refresh_token", refreshToken);

            var content = new FormUrlEncodedContent(parameters);

            try
            {
                var handler = ProxyConfig.CreateClientHandler(ProxyConfig);
                var client = new HttpClient(handler);
                var siteResponse = await client.PostAsync(_exchangeServerUri + endpoint, content);

                var token = JsonConvert.DeserializeObject<Token>(await siteResponse.Content.ReadAsStringAsync());

                if (!string.IsNullOrEmpty(token?.AccessToken) && !token.HasError())
                {
                    SetAccessExpireTimer(token);
                }

                return token;
            }
            catch (HttpRequestException) when (currentRetries < MaxGetTokenRetries)
            {
                currentRetries++;
                await Task.Delay(125 * currentRetries);
                return await GetToken(grantType, endpoint, authorizationCode, refreshToken, currentRetries);
            }
        }

        /// <summary>
        /// If <see cref="TimeAccessExpiry"/> is true, sets a timer for how long access will take to expire.
        /// </summary>
        /// <param name="token"></param>
        private void SetAccessExpireTimer(Token token)
        {
            if (_accessTokenExpireTimer != null)
            {
                _accessTokenExpireTimer.Stop();
                _accessTokenExpireTimer.Dispose();
            }

            if (!TimeAccessExpiry) return;

            _accessTokenExpireTimer = new Timer
            {
                Enabled = true,
                Interval = token.ExpiresIn * 1000,
                AutoReset = false
            };
            _accessTokenExpireTimer.Elapsed += (sender, e) => OnAccessTokenExpired?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Uses the authorization code to silently (doesn't open a browser) obtain both an access token and refresh token, where the refresh token would be required for you to use <see cref="RefreshAuthAsync(string)"/>.
        /// </summary>
        /// <param name="authorizationCode"></param>
        internal async Task<Token> ExchangeCodeAsync(string authorizationCode)
            => await GetToken("authorization_code", "/authorize", authorizationCode);

        /// <summary>
        /// Uses the refresh token to silently (doesn't open a browser) obtain a fresh access token, no refresh token is given however (as it does not change).
        /// </summary>
        /// <param name="refreshToken"></param>
        public async Task<Token> RefreshAuthAsync(string refreshToken)
            => await GetToken("refresh_token", "/refresh", refreshToken: refreshToken);
    }
}