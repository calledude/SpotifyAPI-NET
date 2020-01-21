using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace SpotifyAPI.Web.Auth.Controllers
{
    internal class AuthorizationCodeAuthController : WebApiController
    {
        [Route(HttpVerbs.Get, "/")]
        public Task GetEmpty()
        {
            string state = Request.QueryString["state"];
            AuthorizationCodeAuth.Instances.TryGetValue(state, out SpotifyAuthServer<AuthorizationCodeResponse> auth);

            string code = null;
            string error = Request.QueryString["error"];
            if (error == null)
                code = Request.QueryString["code"];

            AuthorizationCodeResponse authcode = new AuthorizationCodeResponse
            {
                Code = code,
                Error = error
            };

            AuthorizationCodeAuth au = (AuthorizationCodeAuth)auth;

            Task.Factory.StartNew(async () => auth?.TriggerAuth(await au.ExchangeCode(authcode.Code)));

            return HttpContext.SendStandardHtmlAsync(200, (tw) =>
            {
                tw.WriteLine("<script>window.close()</script>");
                tw.Flush();
            });
        }

        [Route(HttpVerbs.Post, "/")]
        public async Task PostValues()
        {
            NameValueCollection formParams = await HttpContext.GetRequestFormDataAsync();

            string state = formParams["state"];
            AuthorizationCodeAuth.Instances.TryGetValue(state, out SpotifyAuthServer<AuthorizationCodeResponse> authServer);

            AuthorizationCodeAuth auth = (AuthorizationCodeAuth)authServer;
            auth.ClientId = formParams["clientId"];
            auth.SecretId = formParams["secretId"];

            string uri = auth.GetUri();
            HttpContext.Redirect(uri);
        }
    }
}
